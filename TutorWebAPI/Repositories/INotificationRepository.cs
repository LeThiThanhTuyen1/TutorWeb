using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetUserNotifications(int userId);
        Task<List<Notification>> GetUnreadNotifications(int userId);
        Task<List<Notification>> GetReadNotifications(int userId);
        Task<bool> MarkAsRead(int notificationId);
        Task<bool> CreateNotification(int userId, string message, string type);
    }
}
