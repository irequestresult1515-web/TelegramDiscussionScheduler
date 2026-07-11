using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramDiscussionScheduler.Models;
using TelegramDiscussionScheduler.Options;

namespace TelegramDiscussionScheduler.Services;

public sealed class DiscussionService : IDiscussionService
{
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;
    private readonly ILogger<DiscussionService> _logger;

    public DiscussionService(
        ITelegramService telegram,
        IOptions<TelegramOptions> options,
        ILogger<DiscussionService> logger)
    {
        _telegram = telegram;
        _options = options.Value;
        _logger = logger;
    }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "══════════════════════════════════════\n" +
            "  Opening discussion across {Count} group(s)...\n" +
            "  TimeZone: {TimeZone}\n" +
            "══════════════════════════════════════",
            _options.Groups.Count, _options.TimeZone);

        foreach (var group in _options.Groups)
        {
            await ProcessGroupOpenAsync(group, cancellationToken);
        }

        _logger.LogInformation("✅ All groups processed for OPEN.");
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "══════════════════════════════════════\n" +
            "  Closing discussion across {Count} group(s)...\n" +
            "  TimeZone: {TimeZone}\n" +
            "══════════════════════════════════════",
            _options.Groups.Count, _options.TimeZone);

        foreach (var group in _options.Groups)
        {
            await ProcessGroupCloseAsync(group, cancellationToken);
        }

        _logger.LogInformation("✅ All groups processed for CLOSE.");
    }

    private async Task ProcessGroupOpenAsync(DiscussionSettings group, CancellationToken ct)
    {
        _logger.LogInformation("──► Opening discussion in group: {GroupName} ({ChatId})",
            group.GroupName, group.ChatId);

        if (string.IsNullOrWhiteSpace(group.ChatId))
        {
            _logger.LogError("❌ Skipping group '{GroupName}': ChatId is empty.", group.GroupName);
            return;
        }

        try
        {
            var messageId = await _telegram.SendMessageAsync(group.ChatId, group.OpenMessage, ct);
            if (messageId is null)
            {
                _logger.LogError("❌ Failed to send open message to {GroupName}.", group.GroupName);
                return;
            }

            if (group.PinOpenMessage)
                await _telegram.PinMessageAsync(group.ChatId, messageId.Value, ct);

            if (group.UnrestrictOnOpen)
                await _telegram.SetChatPermissionsAsync(group.ChatId, canSendMessages: true, ct);

            _logger.LogInformation("✅ Group '{GroupName}' opened successfully.", group.GroupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error opening group '{GroupName}'.", group.GroupName);
        }
    }

    private async Task ProcessGroupCloseAsync(DiscussionSettings group, CancellationToken ct)
    {
        _logger.LogInformation("──► Closing discussion in group: {GroupName} ({ChatId})",
            group.GroupName, group.ChatId);

        if (string.IsNullOrWhiteSpace(group.ChatId))
        {
            _logger.LogError("❌ Skipping group '{GroupName}': ChatId is empty.", group.GroupName);
            return;
        }

        try
        {
            var messageId = await _telegram.SendMessageAsync(group.ChatId, group.CloseMessage, ct);
            if (messageId is null)
            {
                _logger.LogError("❌ Failed to send close message to {GroupName}.", group.GroupName);
                return;
            }

            if (group.PinCloseMessage)
                await _telegram.PinMessageAsync(group.ChatId, messageId.Value, ct);

            if (group.RestrictOnClose)
                await _telegram.SetChatPermissionsAsync(group.ChatId, canSendMessages: false, ct);

            _logger.LogInformation("✅ Group '{GroupName}' closed successfully.", group.GroupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error closing group '{GroupName}'.", group.GroupName);
        }
    }
}