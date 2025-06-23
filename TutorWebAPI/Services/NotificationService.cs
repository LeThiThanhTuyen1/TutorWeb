using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using TutorWebAPI.DTOs;
using TutorWebAPI.Repositories;

namespace TutorWebAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IMemoryCache _cache;
        private readonly int _userId;
        private readonly ILogger<NotificationService> _logger;
        private readonly string _cacheKeyAll;
        private readonly string _cacheKeyUnread;
        private readonly string _cacheKeyRead;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _cache = cache;
            _logger = logger;
            _userId = int.Parse(httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            _cacheKeyAll = $"Notifications_All_{_userId}";
            _cacheKeyUnread = $"Notifications_Unread_{_userId}";
            _cacheKeyRead = $"Notifications_Read_{_userId}";
        }

        public async Task<List<NotificationDTO>> GetUserNotifications()
        {
            _logger.LogInformation("Fetching all notifications for user {UserId}", _userId);

            var data = await _notificationRepository.GetUserNotifications(_userId);
            List<NotificationDTO> notifications = data.Select(n => new NotificationDTO
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                SentAt = n.SentAt,
                IsRead = n.IsRead
            }).ToList();

            return notifications;
        }

        public async Task<List<NotificationDTO>> GetUnreadNotifications()
        {
            _logger.LogInformation("Fetching unread notifications for user {UserId}", _userId);

            var data = await _notificationRepository.GetUnreadNotifications(_userId);
            List<NotificationDTO> notifications = data.Select(n => new NotificationDTO
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                SentAt = n.SentAt,
                IsRead = n.IsRead
            }).ToList();

            return notifications;
        }

        public async Task<List<NotificationDTO>> GetReadNotifications()
        {
            _logger.LogInformation("Fetching read notifications for user {UserId}", _userId);

            var data = await _notificationRepository.GetReadNotifications(_userId);
            List<NotificationDTO> notifications = data.Select(n => new NotificationDTO
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                SentAt = n.SentAt,
                IsRead = n.IsRead
            }).ToList();

            return notifications;
        }

        public async Task<MarkAsReadResult> MarkAsRead(int id)
        {
            _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", id, _userId);

            var success = await _notificationRepository.MarkAsRead(id);
            if (!success)
            {
                _logger.LogWarning("Notification {NotificationId} not found or not belong to user {UserId}", id, _userId);
                return new MarkAsReadResult
                {
                    Success = false,
                    NotificationId = id,
                    Message = "Thông báo không tồn tại hoặc đã bị hủy."
                };
            }

            return new MarkAsReadResult
            {
                Success = true,
                NotificationId = id,
                Message = "Đọc thông báo thành công."
            };
        }
    }
}
