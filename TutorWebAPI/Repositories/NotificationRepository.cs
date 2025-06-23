using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Helper;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<NotificationRepository> logger, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _hub = hub;
        }

        public async Task<List<Notification>> GetUserNotifications(int userId)
        {
            try
            {
                // var cacheKey = $"UserNotifications_{userId}";
                // if (!_cache.TryGetValue(cacheKey, out List<Notification> notifications))
                // {
                var notifications = await _context.Notifications
                              .Where(n => n.UserId == userId)
                              .OrderByDescending(n => n.SentAt)
                              .ToListAsync();

                // _cache.Set(cacheKey, notifications, TimeSpan.FromMinutes(5));
                _logger.LogInformation("Notifications for user {UserId} cached.", userId);
                // }

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving notifications for user {UserId}", userId);
                throw new Exception("An error occurred while retrieving notifications. Please try again later.");
            }
        }

        public async Task<List<Notification>> GetUnreadNotifications(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .OrderByDescending(n => n.SentAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving unread notifications for user {UserId}", userId);
                throw new Exception("An error occurred while retrieving unread notifications. Please try again later.");
            }
        }

        public async Task<List<Notification>> GetReadNotifications(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead)
                    .OrderByDescending(n => n.SentAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving read notifications for user {UserId}", userId);
                throw new Exception("An error occurred while retrieving read notifications. Please try again later.");
            }
        }

        public async Task<bool> MarkAsRead(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                {
                    _logger.LogWarning("Notification {NotificationId} not found.", notificationId);
                    return false;
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification {NotificationId} marked as read.", notificationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while marking notification {NotificationId} as read.", notificationId);
                throw new Exception("An error occurred while marking the notification as read. Please try again later.");
            }
        }

        public async Task<bool> CreateNotification(int userId, string message, string type)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Message = message,
                    Type = type,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("New notification created for user {UserId}.", userId);
                var notificationDto = new NotificationDTO
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Message = notification.Message,
                    Type = notification.Type,
                    SentAt = notification.SentAt,
                    IsRead = notification.IsRead
                };
                _logger.LogInformation("Sending notification: {@Notification}", notificationDto);
                await _hub.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notificationDto);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a notification for user {UserId}.", userId);
                throw new Exception("An error occurred while creating a notification. Please try again later.");
            }
        }
    }
}
