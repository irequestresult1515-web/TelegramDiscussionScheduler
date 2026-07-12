public sealed class DiscussionSettings
{
    public string GroupName { get; set; } = "Default";
    public string ChatId { get; set; } = string.Empty;

    public string OpenMessage { get; set; } = "...";
    public string CloseMessage { get; set; } = "...";

    public bool PinOpenMessage { get; set; }
    public bool PinCloseMessage { get; set; }
    public bool RestrictOnClose { get; set; }
    public bool UnrestrictOnOpen { get; set; }
}
