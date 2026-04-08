namespace Notifications.Notifications;

public interface INotificationHandler
{
    bool HasNotification();
    List<NotificationEvent> GetNotifications();
    void DefaultBuilder(string code, string message, string? detailMessage = null);
    void Clear();
}
