namespace Notifications.Notifications;

public class NotificationEvent
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DetailMessage { get; set; }
}
