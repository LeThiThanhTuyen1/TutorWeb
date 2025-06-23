using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorWebAPI.Models;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using System.Security.Claims;

namespace TutorWebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CourseController> _logger;
        private readonly HttpClient _httpClient;

        public CourseController(ICourseService courseService, ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all available courses.
        /// </summary>
        [HttpGet("Courses")]
        public async Task<IActionResult> GetAllCourses(
            [FromQuery] PaginationFilter filter,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? statuses = null)
        {
            var route = Request.Path.Value;
            var statusList = string.IsNullOrWhiteSpace(statuses)
                ? null
                : statuses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim().ToLower())
                          .ToList();
            var pagedCourses = await _courseService.GetAllCoursesAsync(filter, route, searchTerm, statusList);
            return Ok(pagedCourses);
        }

        /// <summary>
        /// Retrieves courses by tutor ID.
        /// </summary>
        [HttpGet("Courses/tutor")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> GetTutorCoursesByUserId([FromQuery] PaginationFilter filter)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var route = Request.Path.Value;
                var courses = await _courseService.GetTutorCoursesByUserId(filter, route, userId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi danh sách khóa học với userID {userId}.", userId);
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }

        /// <summary>
        /// Get paginated list of students by user id
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>Paginated list of students by user id</returns>
        [Authorize(Roles = "Student")]
        [HttpGet("Courses/student")]
        public async Task<IActionResult> GetStudentCoursesByUserId([FromQuery] PaginationFilter filter)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var route = Request.Path.Value;
                var courses = await _courseService.GetStudentCoursesByUserId(filter, route, userId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lây danh sách khóa học với UserID: {userId}.", userId);
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }

        /// <summary>
        /// Retrieves a course by its ID.
        /// </summary>
        [HttpGet("Course/{id}")]
        public async Task<IActionResult> GetCourseById(int id)
        {
            try
            {
                var courseDto = await _courseService.GetCourseByIdAsync(id);
                if (courseDto == null)
                {
                    _logger.LogWarning("Khóa học với ID {CourseId} không tồn tại.", id);
                    return NotFound(new Response<string>("Khóa học không tồn tại."));
                }

                return Ok(new Response<CourseDTO>(courseDto) { Message = "Thông tin khóa học được lấy thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khóa học ID {CourseId}.", id);
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="learnerId"></param>
        ///// <returns></returns>
        //[Authorize(Roles = "Student")]
        //[HttpGet("{userId}")]
        //public async Task<IActionResult> GetRecommendations(int userId)
        //{
        //    var mlApiUrl = "https://tutorrecommendation.eastus.azurecontainer.io/score";
        //    var apiKey = "abc123XYZ";

        //    var requestData = new
        //    {
        //        Inputs = new
        //        {
        //            data = new[] {
        //        new { user_id = userId }
        //    }
        //        }
        //    };

        //    var request = new HttpRequestMessage(HttpMethod.Post, mlApiUrl)
        //    {
        //        Content = JsonContent.Create(requestData)
        //    };
        //    request.Headers.Add("Authorization", $"Bearer {apiKey}");

        //    var response = await _httpClient.SendAsync(request);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        var errorContent = await response.Content.ReadAsStringAsync();
        //        return StatusCode(500, $"ML model error: {errorContent}");
        //    }

        //    var recommendations = await response.Content.ReadFromJsonAsync<object>();
        //    return Ok(recommendations);
        //}


        /// <summary>
        /// Creates a new course.
        /// </summary>
        [HttpPost("Course")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> CreateCourse([FromBody] CourseDTO courseDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();

                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = errors
                });
            }

            try
            {
                var result = await _courseService.AddCourseAsync(userId, courseDto);
                return CreatedAtAction(nameof(GetCourseById), new { id = result.Id },
                    new Response<CourseDTO>(result)
                    {
                        Succeeded = true,
                        Message = "Tạo mới khóa học thành công",
                        Errors = null
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new Response<string>
                {
                    Succeeded = false,
                    Message = ex.Message,
                    Errors = new[] { ex.Message }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new Response<string>
                {
                    Succeeded = false,
                    Message = ex.Message,
                    Errors = new[] { ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo mới khóa học.");
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi khi tạo mới khóa học.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Updates an existing course by its ID.
        /// </summary>
        [HttpPut("Course/{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course course)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();

                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = errors
                });
            }

            try
            {
                var courseUpdated = await _courseService.UpdateCourseAsync(id, course);
                return Ok(new Response<CourseDTO>
                {
                    Succeeded = true,
                    Message = "Cập nhật thông tin khóa học thành công.",
                    Data = courseUpdated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật thông tin khóa học với ID {CourseId}.", id);
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi cập nhật thông tin khóa học.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Cancels a course by its ID.
        /// </summary>
        [HttpPost("Course/{id}/cancel")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> CancelCourse(int id)
        {
            try
            {
                await _courseService.CancelCourseAsync(id);
                return Ok(new Response<string> { Succeeded = true, Message = "Hủy khóa học thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new Response<string>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy khóa học với ID {CourseId}.", id);
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }

        /// <summary>
        /// Retrieves all students enrolled in a course.
        /// </summary>
        [HttpGet("Course/{id}/students")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> GetStudentsByCourseId(int id)
        {
            try
            {
                var students = await _courseService.GetStudentsByCourseIdAsync(id);
                if (students == null || students.Count == 0)
                {
                    return NotFound(new Response<string>("Chưa ai đăng ký khóa học này."));
                }

                return Ok(new Response<List<StudentDTO>>(students) { Message = "Lấy danh sách đăng ký thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy thông tin khóa học với ID {CourseId}.", id);
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }

        /// <summary>
        /// Deletes multiple courses by their IDs.
        /// </summary>
        [HttpDelete("Courses")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteCourses([FromBody] List<int> courseIds)
        {
            if (courseIds == null || courseIds.Count == 0)
                return BadRequest(new Response<string>("Danh sách khóa học không hợp lệ."));

            try
            {
                var deleteResult = await _courseService.DeleteCoursesAsync(courseIds);

                if (deleteResult)
                {
                    return Ok(new Response<string> { Succeeded = true, Message = "Xóa khóa học thành công." });
                }
                else
                {
                    return BadRequest(new Response<string>("Lỗi khi xóa khóa học."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa khóa học.");
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }
    }
}
