namespace TelegramDiscussionScheduler.Services;

public interface IDiscussionService
{
    Task OpenAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}