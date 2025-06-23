using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class TutorsController : ControllerBase
    {
        private readonly ITutorService _tutorService;
        private readonly ILogger<TutorsController> _logger;
        public TutorsController(ITutorService tutorService, ILogger<TutorsController> logger)
        {
            _tutorService = tutorService;
            _logger = logger;
        }

        /// <summary>
        /// Search for tutors based on search criteria provided by the user.
        /// </summary>
        /// <param name="searchCriteria">An object containing the search criteria for finding suitable tutors.</param>
        /// <param name="filter">An object containing page size and page number for pagination.</param>
        /// <returns>
        /// Returns a list of tutors that match the search criteria.
        /// </returns>
        /// <response code="200">Returns a list paginated of tutors matching the search criteria.</response>
        /// <response code="404">Returns if no tutor matches the search criteria.</response>
        [HttpGet("Tutors/search")]
        public async Task<IActionResult> SearchTutors([FromQuery] TutorSearchDTO searchCriteria, [FromQuery] PaginationFilter filter)
        {
            _logger.LogInformation("Tìm chí gia sư với tiêu chí: {@SearchCriteria}", searchCriteria);

            var route = Request.Path.Value;
            var result = await _tutorService.SearchTutors(searchCriteria, filter, route);

            if (result.Data == null || result.Data.Count == 0)
                return NotFound(new Response<string>("Không tìm thấy gia sư phù hợp với tiêu chí của bạn."));

            return Ok(result);
        }

        /// <summary>
        /// Get all tutors.
        /// </summary>
        [HttpGet("Tutors")]
        public async Task<IActionResult> GetAllTutors([FromQuery] PaginationFilter filter)
        {
            _logger.LogInformation("Tải danh sách gia sư đã phân trang.");

            var route = Request.Path.Value;
            var result = await _tutorService.GetAllTutorsAsync(filter, route);

            if (result.Data == null || result.Data.Count == 0)
                return NotFound(new Response<string>("Lỗi lấy danh sách gia sư."));

            return Ok(result);
        }

        ///// <summary>
        ///// Get tutor details by user ID.
        ///// </summary>
        ///// <returns></returns>
        //[HttpGet("TutorsByUserId")]
        //public async Task<IActionResult> GetTutorByUserId()
        //{
        //    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        //    _logger.LogInformation("Fetching tutor with User ID: {UserId}", userId);

        //    var tutor = await _tutorService.GetTutorByUserIdAsync(userId);

        //    if (tutor == null)
        //        return NotFound(new Response<string>($"No tutor found for user ID {userId}."));

        //    return Ok(new Response<TutorDTO>(tutor));
        //}

        /// <summary>
        /// Get tutor details by user ID.
        /// </summary>
        [HttpGet("Tutor/{tutorId}")]
        public async Task<IActionResult> GetTutorByUserId(int tutorId)
        {
            _logger.LogInformation("Lỗi khi lấy danh sách gia sư theo user ID: {UserId}", tutorId);

            var tutor = await _tutorService.GetTutorByIdAsync(tutorId);

            if (tutor == null)
                return NotFound(new Response<string>($"Không thể tìm thấy gia sư với user ID {tutorId}."));

            return Ok(new Response<TutorDTO>(tutor));
        }

        /// <summary>
        /// Get statistics based on tutor data
        /// </summary>
        /// <returns>Statistics result</returns>
        [Authorize(Roles = "Tutor")]
        [HttpGet("Tutor/dashboard")]
        public async Task<ActionResult<Response<object>>> GetDashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var dashboardData = await _tutorService.GetDashboardDataAsync(userId);
                return Ok(new Response<object>(dashboardData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<object>
                {
                    Succeeded = false,
                    Message = "Lỗi khi lấy danh sách gia sư.",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
