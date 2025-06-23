using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;
using Microsoft.Extensions.Logging;

namespace TutorWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// Get admin dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var dashboardData = await _adminService.GetDashboardDataAsync();
                return Ok(new Response<AdminDashboardResponse>(dashboardData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu.");
                return StatusCode(500, new Response<AdminDashboardResponse>
                {
                    Succeeded = false,
                    Message = "Có lỗi không mong muốn trong quá trình lấy dữ liệu.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Lỗi khi lấy danh sách người dùng trang {Page} với danh sách là {PageSize}", page, pageSize);

            try
            {
                var filter = new PaginationFilter(page, pageSize);
                var route = Request.Path.Value;
                var users = await _adminService.GetPagedUsersAsync(filter, route);
                return Ok(new Response<PagedResponse<List<User>>>(users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách người dùng đã phân trang.");
                return StatusCode(500, new Response<PagedResponse<List<User>>>
                {
                    Succeeded = false,
                    Message = "Có lỗi không mong muốn xảy ra khi lấy danh sách người dùng.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Add new user
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> AddUser([FromBody] User userInfo)
        {
            _logger.LogInformation("Thêm mới người dùng với email: {Email}", userInfo.Email);

            try
            {
                var result = await _adminService.AddUserAsync(userInfo);
                if (!result.Succeeded)
                    return BadRequest(new Response<User>(result.Message));

                return Ok(new Response<User>
                {
                    Succeeded = true,
                    Message = "Thêm người dùng thành công.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm người dùng.");
                return StatusCode(500, new Response<User>
                {
                    Succeeded = false,
                    Message = "Có lỗi không mong muốn xảy ra khi thêm người dùng.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update user by ID
        /// </summary>
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User userInfo)
        {
            _logger.LogInformation("Cập nhật người dùng với ID: {UserId}", id);

            try
            {
                var result = await _adminService.UpdateUserAsync(id, userInfo);
                if (!result.Succeeded)
                    return BadRequest(new Response<User>(result.Message));

                return Ok(new Response<User>
                {
                    Succeeded = true,
                    Message = "Cập nhật người dùng thành công.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật người dùng.");
                return StatusCode(500, new Response<User>
                {
                    Succeeded = false,
                    Message = "Có lỗi không mong muốn xảy ra khi cập nhật người dùng.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete users by IDs
        /// </summary>
        [HttpDelete("users")]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            _logger.LogInformation("Xóa danh sách người dùng với IDs: {@UserIds}", userIds);

            try
            {
                var result = await _adminService.DeleteUsersAsync(userIds);

                if (!result.Succeeded)
                    return BadRequest(new Response<string>(result.Message));

                return Ok(new Response<string>
                {
                    Succeeded = true,
                    Message = "Có lỗi không mong muốn xảy ra khi xóa người dùng.",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa người dùng.");
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Có lỗi không mong muốn xảy ra khi xóa người dùng.",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}