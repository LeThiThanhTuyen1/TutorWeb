using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.DTOs;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get a list of unread notifications
        /// </summary>
        /// <returns>List of unread notifications</returns>
        [HttpGet("Notifications")]
        public async Task<IActionResult> GetUserNotifications()
        {
            var notifications = await _notificationService.GetUserNotifications();

            if (notifications == null || !notifications.Any())
            {
                return NotFound(new Response<List<object>>
                {
                    Succeeded = false,
                    Message = "Không tìm thấy thông báo.",
                    Errors = null,
                    Data = null
                });
            }

            return Ok(new Response<List<NotificationDTO>>
            {
                Succeeded = true,
                Message = "Lấy danh sách thông báo thành công.",
                Data = notifications
            });
        }

        /// <summary>
        /// Get a list of unread notifications
        /// </summary>
        /// <returns>Lists unread notifications if found, and returns an empty list if not found.</returns>
        [HttpGet("Notifications/unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var notifications = await _notificationService.GetUnreadNotifications();

            if (notifications == null || !notifications.Any())
            {
                return NotFound(new Response<List<object>>
                {
                    Succeeded = false,
                    Message = "Không tìm thấy thông báo.",
                    Errors = null,
                    Data = null
                });
            }

            return Ok(new Response<List<NotificationDTO>>
            {
                Succeeded = true,
                Message = "Lấy thông báo thành công.",
                Data = notifications
            });
        }

        /// <summary>
        /// Get list of notifications that have been read
        /// </summary>
        /// <returns>Lists notifications that have been read if found, and returns an empty list if not found.</returns>
        [HttpGet("Notifications/read")]
        public async Task<IActionResult> GetReadNotifications()
        {
            var notifications = await _notificationService.GetReadNotifications();

            if (notifications == null || !notifications.Any())
            {
                return NotFound(new Response<List<object>>
                {
                    Succeeded = false,
                    Message = "Không tìm thấy thông báo.",
                    Errors = null,
                    Data = null
                });
            }

            return Ok(new Response<List<NotificationDTO>>
            {
                Succeeded = true,
                Message = "Lấy thông báo thành công.",
                Data = notifications
            });
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        /// <param name="id">Unique notification id</param>
        /// <returns>Ok with type response</returns>
        [HttpPost("Notification/mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsRead(id);

            if (!result.Success)
            {
                return NotFound(new Response<MarkAsReadResult>
                {
                    Succeeded = false,
                    Message = result.Message,
                    Errors = null,
                    Data = result
                });
            }

            return Ok(new Response<MarkAsReadResult>
            {
                Succeeded = true,
                Message = result.Message,
                Data = result
            });
        }
    }
}
