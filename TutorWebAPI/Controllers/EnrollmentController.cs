using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.Repositories;
using TutorWebAPI.DTOs;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[Route("api")]
[ApiController]
[Authorize(Roles = "Student")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService, IUriRepository iUriRepository)
    {
        _enrollmentService = enrollmentService;
    }

    /// <summary>
    /// Registers a student for a course.
    /// </summary>
    /// <param name="request">The enrollment request course IDs.</param>
    /// <returns>A Response object indicating the result of the registration process.</returns>
    /// <response code="200">If registration is successful and contract is created.</response>
    /// <response code="400">If the request is invalid, the student is ineligible, or there is a schedule conflict.</response>
    /// <response code="404">If the course does not exist.</response>
    [HttpPost("Enrollment/register")]
    public async Task<ActionResult<Response<string>>> RegisterCourse([FromBody] EnrollmentRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var response = await _enrollmentService.RegisterStudentForCourse(request.CourseId, userId);

        if (!response.Succeeded)
        {
            if (response.Errors != null && response.Errors.Contains("Lỗi hệ thống."))
                return StatusCode(500, response);

            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Canceling a registered course
    /// </summary>
    /// <param name="request"></param>
    /// <returns>Cancel result</returns>
    [HttpDelete("Enrollment/unenroll")]
    public async Task<IActionResult> UnenrollStudent([FromBody] EnrollmentRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _enrollmentService.UnenrollStudentAsync(userId, request.CourseId);
            if (!result)
                return BadRequest(new Response<string>("Không thể hủy đăng ký khóa học ở trạng thái 'Đã hủy'."));

            return Ok(new Response<string>
            {
                Succeeded = true,
                Message = "Hủy đăng ký khóa học thành công"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new Response<string>("Lỗi khi hủy đăng ký khóa học."));
        }
    }

    /// <summary>
    /// Get enrollment information by id
    /// </summary>
    /// <param name="enrollmentId">Enrollment Id</param>
    /// <returns> enrollment information</returns>
    [HttpGet("Enrollment")]
    public async Task<ActionResult<Response<string>>> GetEnrollmentById(int enrollmentId)
    {
        var response = await _enrollmentService.GetEnrollmentById(enrollmentId);

        if (response == null)
        {
            return BadRequest(new Response<string>("Đăng ký không tồn tại."));
        }

        return Ok(response);
    }
}
