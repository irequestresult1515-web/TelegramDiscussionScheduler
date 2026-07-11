using System.ComponentModel.DataAnnotations;
using TelegramDiscussionScheduler.Models;

namespace TelegramDiscussionScheduler.Options;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    [Required]
    public string BotToken { get; init; } = string.Empty;

    public string TimeZone { get; init; } = "Asia/Damascus";

    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(1, 60)]
    public int RetryDelaySeconds { get; init; } = 2;

    public List<DiscussionSettings> Groups { get; init; } = new()
    {
        new DiscussionSettings()
    };
}