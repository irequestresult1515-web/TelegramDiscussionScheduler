using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramDiscussionScheduler.Options;

namespace TelegramDiscussionScheduler.Services;

public sealed class TelegramService : ITelegramService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramService> _logger;
    private readonly string _botToken;
    private readonly int _maxRetries;
    private readonly int _delaySeconds;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TelegramService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _botToken = options.Value.BotToken;
        _maxRetries = options.Value.MaxRetryAttempts;
        _delaySeconds = options.Value.RetryDelaySeconds;
    }

    public async Task<int?> SendMessageAsync(
        string chatId,
        string message,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async ct =>
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            var payload = new
            {
                chat_id = chatId,
                text = message,
                disable_web_page_preview = true
            };

            _logger.LogInformation("Sending message to chat {ChatId}...", chatId);

            var response = await _httpClient.PostAsJsonAsync(url, payload, JsonOptions, ct);
            var result = await ParseResponseAsync<SendMessageResponse>(response, ct);

            if (result is { Ok: true, Result: not null })
            {
                _logger.LogInformation(
                    "Message sent successfully to chat {ChatId}. MessageId: {MessageId}",
                    chatId, result.Result.MessageId);
                return result.Result.MessageId;
            }

            throw new TelegramApiException(
                result?.Description ?? "Unknown error",
                result?.ErrorCode);
        }, cancellationToken);
    }

    public async Task<bool> PinMessageAsync(
        string chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async ct =>
        {
            var url = $"https://api.telegram.org/bot{_botToken}/pinChatMessage";

            var payload = new
            {
                chat_id = chatId,
                message_id = messageId,
                disable_notification = true
            };

            _logger.LogInformation(
                "Pinning message {MessageId} in chat {ChatId}...", messageId, chatId);

            var response = await _httpClient.PostAsJsonAsync(url, payload, JsonOptions, ct);
            var result = await ParseResponseAsync<TelegramResponse>(response, ct);

            if (result is { Ok: true })
            {
                _logger.LogInformation(
                    "Message {MessageId} pinned successfully in chat {ChatId}.",
                    messageId, chatId);
                return true;
            }

            throw new TelegramApiException(
                result?.Description ?? "Unknown error",
                result?.ErrorCode);
        }, cancellationToken);
    }

    public async Task<bool> SetChatPermissionsAsync(
        string chatId,
        bool canSendMessages,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async ct =>
        {
            var url = $"https://api.telegram.org/bot{_botToken}/setChatPermissions";

            var payload = new
            {
                chat_id = chatId,
                permissions = new
                {
                    can_send_messages = canSendMessages,
                    can_send_photos = canSendMessages,
                    can_send_videos = canSendMessages,
                    can_send_other_messages = canSendMessages,
                    can_add_web_page_previews = canSendMessages,
                    can_send_polls = canSendMessages,
                    can_invite_users = false,
                    can_change_info = false,
                    can_pin_messages = false,
                    can_manage_topics = false
                }
            };

            var action = canSendMessages ? "Unrestricting" : "Restricting";
            _logger.LogInformation("{Action} chat {ChatId}...", action, chatId);

            var response = await _httpClient.PostAsJsonAsync(url, payload, JsonOptions, ct);
            var result = await ParseResponseAsync<TelegramResponse>(response, ct);

            if (result is { Ok: true })
            {
                _logger.LogInformation(
                    "Chat {ChatId} permissions updated: canSendMessages={CanSend}",
                    chatId, canSendMessages);
                return true;
            }

            throw new TelegramApiException(
                result?.Description ?? "Unknown error",
                result?.ErrorCode);
        }, cancellationToken);
    }

    // ── Retry logic ──────────────────────────────────────────────────

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct)
    {
        int totalAttempts = _maxRetries + 1; // initial attempt + retries

        for (int attempt = 1; attempt <= totalAttempts; attempt++)
        {
            try
            {
                return await action(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException
                                        or TaskCanceledException
                                        or TelegramApiException)
            {
                if (attempt == totalAttempts)
                {
                    _logger.LogError(ex,
                        "All {TotalAttempts} attempts exhausted ({Retries} retries).",
                        totalAttempts, _maxRetries);
                    throw;
                }

                var delay = TimeSpan.FromSeconds(_delaySeconds * Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex,
                    "Telegram API call failed (attempt {Attempt}/{TotalAttempts}). " +
                    "Waiting {Delay:F1}s before retry...",
                    attempt, totalAttempts, delay.TotalSeconds);

                await Task.Delay(delay, ct);
            }
        }

        throw new InvalidOperationException("Unreachable: retry loop exited unexpectedly.");
    }

    private static async Task<T?> ParseResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken ct) where T : TelegramResponse
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Telegram API returned HTTP {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return result;
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────

public class TelegramResponse
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("error_code")] public int? ErrorCode { get; init; }
}

public sealed class SendMessageResponse : TelegramResponse
{
    [JsonPropertyName("result")] public SendMessageResult? Result { get; init; }
}

public sealed class SendMessageResult
{
    [JsonPropertyName("message_id")] public int MessageId { get; init; }
}

public sealed class TelegramApiException : Exception
{
    public int? ErrorCode { get; }
    public TelegramApiException(string message, int? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}