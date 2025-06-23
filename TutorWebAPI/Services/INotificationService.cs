
using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDTO>> GetUserNotifications();
        Task<List<NotificationDTO>> GetUnreadNotifications();
        Task<List<NotificationDTO>> GetReadNotifications();
        Task<MarkAsReadResult> MarkAsRead(int id);
    }
}