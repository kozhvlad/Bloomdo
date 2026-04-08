namespace Bloomdo.Shared.DTOs.Chat;

public sealed class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public TodayLocalContext? TodayContext { get; set; }
}
