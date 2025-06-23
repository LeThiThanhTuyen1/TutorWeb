using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;
using TutorWebAPI.Services;
using System.Security.Claims;

[Route("api")]
[ApiController]
[Authorize]
public class ComplaintController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly ILogger<ComplaintController> _logger;

    public ComplaintController(IComplaintService complaintService, ILogger<ComplaintController> logger)
    {
        _complaintService = complaintService;
        _logger = logger;
    }

    /// <summary>
    /// Fetches all complaints.
    /// </summary>
    /// <returns>A list of all complaints.</returns>
    /// <response code="200">Returns the list of complaints.</response>
    [HttpGet("Complaints")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllComplaints([FromQuery] PaginationFilter filter)
    {
        var route = Request.Path.Value;
        var response = await _complaintService.GetAllComplaintsAsync(filter, route);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new complaint.
    /// </summary>
    /// <param name="complaint">Complaint object to create.</param>
    /// <returns>The created complaint.</returns>
    /// <response code="201">Returns the newly created complaint.</response>
    /// <response code="400">If the complaint data is invalid.</response>
    [HttpPost("Complaint")]
    public async Task<IActionResult> CreateComplaint([FromBody] Complaint complaint)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (complaint == null || string.IsNullOrEmpty(complaint.Description))
        {
            _logger.LogWarning("Thông tin không hợp lệ.");
            return BadRequest(new Response<string>("Thông tin không hợp lệ"));
        }

        complaint.UserId = userId;
        try
        {
            var complaintDto = await _complaintService.CreateComplaintAsync(complaint);
            _logger.LogInformation("Khiếu nại thành công với ID {ComplaintId}.", complaintDto.Id);
            return CreatedAtAction(nameof(GetComplaintById), new { id = complaintDto.Id }, new Response<ComplaintDTO>(complaintDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi khiếu nại.");
            return StatusCode(500, new Response<string>("Lỗi khi khiếu nại."));
        }
    }

    /// <summary>
    /// Fetches a specific complaint by its ID.
    /// </summary>
    /// <param name="id">The ID of the complaint.</param>
    /// <returns>The complaint data for the specified ID.</returns>
    /// <response code="200">Returns the complaint details.</response>
    /// <response code="404">If the complaint is not found.</response>
    [HttpGet("Complaint/{id}")]
    public async Task<IActionResult> GetComplaintById(int id)
    {
        var complaint = await _complaintService.GetComplaintByIdAsync(id);
        _logger.LogInformation("Lấy khiếu nại với ID: {0}", id);

        if (complaint == null)
        {
            _logger.LogWarning("Không tìm thấy khiếu nại với ID {0}.", id);
            return NotFound(new Response<string>("Không tìm thấy khiếu nại."));
        }

        return Ok(new Response<ComplaintDTO>(complaint));
    }

    /// <summary>
    /// Processes a complaint by approving or rejecting it.
    /// </summary>
    /// <param name="complaintId">The ID of the complaint to process.</param>
    /// <param name="request">The action request containing the processing action ("approve" or "reject").</param>
    /// <returns>A message indicating the result of the processing action.</returns>
    /// <response code="200">If the complaint has been successfully processed.</response>
    /// <response code="400">If the complaint has no valid contract, the action is invalid, or the complaint can't be processed.</response>
    /// <response code="404">If the complaint is not found.</response>
    [HttpPost("Complaint/{complaintId}/process")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessComplaint(int complaintId, [FromBody] ComplaintActionRequest request)
    {
        bool result = await _complaintService.ProcessComplaintAsync(complaintId, request.Action);
        _logger.LogInformation("Xử lý khiếu nại với ID {1}", complaintId);

        if (!result)
        {
            _logger.LogWarning("Hành động không hợp lệ: {0} hoặc khiếu nại với ID {1} không tồn tại.", request.Action, complaintId);
            return BadRequest(new Response<string>("Hành động không hợp lệ hoặc không tìm thấy khiếu nại."));
        }

        return Ok(new Response<string>
        {
            Succeeded = true,
            Message = "Duyệt khiếu nại thành công.",
            Data = null
        });
    }
}