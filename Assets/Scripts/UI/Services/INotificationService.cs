using UnityEngine.UIElements;

namespace UI.Services
{
    public enum NotificationType
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Success = 3
    }

    public enum NotificationDisplayType
    {
        Inline = 0,
        Popup = 1,
        Toast = 2
    }

    public enum NotificationPosition
    {
        TopRight = 0,
        TopLeft = 1,
        TopCenter = 2,
        BottomCenter = 3,
        BottomRight = 4,
        BottomLeft = 5,
        Inline = 6
    }

    public interface INotificationService
    {
        void ShowNotification(
            string message,
            NotificationType type = NotificationType.Info,
            NotificationDisplayType displayType = NotificationDisplayType.Popup,
            NotificationPosition position = NotificationPosition.TopRight,
            float duration = -1,
            string title = null
        );
        
        void ShowInlineNotification(string message, 
            NotificationType type = NotificationType.Info, float duration = -1);
            
        void ShowPopupNotification(string message, 
            NotificationType type = NotificationType.Info, 
            NotificationPosition position = NotificationPosition.TopRight,
            float duration = -1, string title = null);
            
        void ShowToastNotification(string message, 
            NotificationType type = NotificationType.Info,
            NotificationPosition position = NotificationPosition.BottomRight,
            float duration = -1);
            
        void ClearNotifications(NotificationType? type = null, 
            NotificationDisplayType? displayType = null);
    }
}