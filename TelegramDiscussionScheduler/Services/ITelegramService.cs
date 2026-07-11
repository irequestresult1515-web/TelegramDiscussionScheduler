namespace TelegramDiscussionScheduler.Services;

public interface ITelegramService
{
    Task<int?> SendMessageAsync(string chatId, string message, CancellationToken cancellationToken = default);
    Task<bool> PinMessageAsync(string chatId, int messageId, CancellationToken cancellationToken = default);
    Task<bool> SetChatPermissionsAsync(string chatId, bool canSendMessages, CancellationToken cancellationToken = default);
}