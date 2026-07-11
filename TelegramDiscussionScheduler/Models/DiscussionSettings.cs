namespace TelegramDiscussionScheduler.Models;

public sealed class DiscussionSettings
{
    public string GroupName { get; init; } = "Default";
    public string ChatId { get; init; } = string.Empty;

    public string OpenMessage { get; init; } =
""""
السلام عليكم ورحمة الله وبركاته

تم فتح كروب النقاش لاستقبال أسئلتكم واستفساراتكم.

وبالتوفيق جميعًا 🤍
"""";

    public string CloseMessage { get; init; } =
"""
السلام عليكم ورحمة الله وبركاته

تم إغلاق كروب النقاش مؤقتًا.

سيتم إعادة فتحه في الموعد المحدد.

وبالتوفيق جميعًا 🤍
""";
    public bool PinOpenMessage { get; init; } = false;
    public bool PinCloseMessage { get; init; } = false;
    public bool RestrictOnClose { get; init; } = false;
    public bool UnrestrictOnOpen { get; init; } = false;
}