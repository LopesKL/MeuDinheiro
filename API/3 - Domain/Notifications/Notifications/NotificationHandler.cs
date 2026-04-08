namespace Notifications.Notifications;

public class NotificationHandler : INotificationHandler
{
    private readonly List<NotificationEvent> _notifications = new();

    public bool HasNotification()
    {
        return _notifications.Any();
    }

    public List<NotificationEvent> GetNotifications()
    {
        return _notifications;
    }

    public void DefaultBuilder(string code, string message, string? detailMessage = null)
    {
        _notifications.Add(new NotificationEvent
        {
            Code = code,
            Message = message,
            DetailMessage = detailMessage
        });
    }

    public void Clear()
    {
        _notifications.Clear();
    }
}
