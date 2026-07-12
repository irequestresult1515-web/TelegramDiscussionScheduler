using System.ComponentModel.DataAnnotations;

namespace TelegramDiscussionScheduler.Options;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    [Required]
    public string BotToken { get; set; } = string.Empty;

    public string TimeZone { get; set; } = "Asia/Damascus";

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 2;

    public List<DiscussionSettings> Groups { get; set; } = new();
}
