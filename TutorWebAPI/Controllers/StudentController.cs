using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.DTOs;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;
using static TutorWebAPI.Services.StudentService;

namespace TutorWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentController> _logger;
        private readonly IMLTrainingService _mlTrainingService;

        public StudentController(IMLTrainingService mlTrainingService, IStudentService studentService, ILogger<StudentController> logger)
        {
            _studentService = studentService;
            _logger = logger; 
            _mlTrainingService = mlTrainingService;
        }

        /// <summary>
        /// Get statistics based on student data
        /// </summary>
        /// <returns>statistics result</returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var stats = await _studentService.GetStudentStatsAsync(userId);
                return Ok(new Response<StudentStatsDTO>(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student stats");
                return StatusCode(500, new Response<string>("Internal server error") { Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Get subject statistics based on student data
        /// </summary>
        /// <returns>Subject statistics</returns>
        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var subjects = await _studentService.GetSubjectDistributionAsync(userId);
                return Ok(new Response<List<SubjectPieDTO>>(subjects));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student subjects");
                return StatusCode(500, new Response<string>("Internal server error") { Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Get courses statistics based on student data
        /// </summary>
        /// <returns>Courses statistics</returns>
        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var courses = await _studentService.GetStudentCoursesAsync(studentId);
                return Ok(new Response<List<StudentCourseDTO>>(courses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student courses");
                return StatusCode(500, new Response<string>("Internal server error") { Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Get tutors statistics based on student data
        /// </summary>
        /// <returns>Tutors statistics</returns>
        [HttpGet("tutors")]
        public async Task<IActionResult> GetTutors()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var tutors = await _studentService.GetStudentTutorsAsync(userId);
                return Ok(new Response<List<TutorDTO>>(tutors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách gia sư.");
                return StatusCode(500, new Response<string>("Lỗi server.") { Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Get statistics public based on system data
        /// </summary>
        /// <returns>Statistics result</returns>
        [HttpGet("stats/public")]
        [AllowAnonymous]
        public async Task<ActionResult<List<StatDTO>>> GetStatsPubblic()
        {
            var stats = await _studentService.GetStatsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("train")]
        public IActionResult TrainModel()
        {
            try
            {
                _mlTrainingService.TrainModel();
                return Ok(new Response<string>("Model trained successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Error training model.",
                    Errors = new[] { ex.Message },
                    Data = null
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="studentId"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        [HttpGet("recommendations/tutors")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTutorRecommendations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var recommendations = await _studentService.GetTutorRecommendationsAsync(userId);
                return Ok(new Response<List<object>>(recommendations));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new Response<object>
                {
                    Succeeded = false,
                    Message = ex.Message,
                    Errors = null,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<object>
                {
                    Succeeded = false,
                    Message = "Error retrieving recommendations.",
                    Errors = new[] { ex.Message },
                    Data = null
                });
            }
        }
    }
}
