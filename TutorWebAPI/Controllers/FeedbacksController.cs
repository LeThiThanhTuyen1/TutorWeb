using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TutorWebAPI.DTOs;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;
using TutorWebAPI.Models;

namespace TutorWebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbacksController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// add a new user feedback
        /// </summary>
        /// <param name="feedback">Feedback information</param>
        /// <returns>Add result according to model response</returns>
        [Authorize(Roles = "Student")]
        [HttpPost("Student/feedback")]
        public async Task<IActionResult> AddFeedback([FromBody] Feedback feedback)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            feedback.StudentId = studentId;

            try
            {
                var result = await _feedbackService.AddFeedbackAsync(feedback);
                if (!result)
                    return BadRequest(new Response<string> { Succeeded = false, Message = "Dữ liệu không hợp lệ." });

                return Ok(new Response<string> { Succeeded = true, Message = "Thêm đánh giá thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = "Lỗi hệ thống", Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Update user feedback
        /// </summary>
        /// <param name="feedbackId">Unique feedback id</param>
        /// <param name="feedback">Feedback information via feedback id</param>
        /// <returns>Update result according to model response</returns>
        [Authorize(Roles = "Student")]
        [HttpPut("Student/feedback/{feedbackId}")]
        public async Task<IActionResult> UpdateFeedback(int feedbackId, [FromBody] Feedback feedback)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            feedback.StudentId = studentId;

            try
            {
                var result = await _feedbackService.UpdateFeedbackAsync(feedbackId, feedback);
                if (!result)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Không tìm thấy đánh giá." });

                return Ok(new Response<string> { Succeeded = true, Message = "Cập nhật đánh giá thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = "Lỗi hệ thống", Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Delete user feedback
        /// </summary>
        /// <param name="feedbackId">Unique feedback Id</param>
        /// <returns>Delete result</returns>
        [Authorize(Roles = "Student")]
        [HttpDelete("Student/feedback/{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var result = await _feedbackService.DeleteFeedbackAsync(feedbackId, studentId);
                if (!result)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Không đánh giá nào được tìm thấy." });

                return Ok(new Response<string> { Succeeded = true, Message = "Xóa đánh giá thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = "Lỗi hệ thống", Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Get list of user feedback with tutor by tutor id
        /// </summary>
        /// <param name="tutorId">Tutor Id</param>
        /// <returns>List of user feedback with tutor</returns>
        [HttpGet("Feedbacks/tutors/{tutorId}")]
        public async Task<IActionResult> GetTutorFeedbacks(int tutorId)
        {
            if (tutorId <= 0)
                return BadRequest(new Response<string> { Succeeded = false, Message = "Dữ liệu không hợp lệ." });

            try
            {
                var feedbacks = await _feedbackService.GetTutorFeedbacksAsync(tutorId);
                if (feedbacks == null || feedbacks.Count == 0)
                    return NotFound(new Response<List<FeedbackDTO>> { Succeeded = false, Message = "Gia sư chưa có đánh giá nào." });

                return Ok(new Response<List<FeedbackDTO>> { Succeeded = true, Data = feedbacks });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = "Lỗi hệ thống", Errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Get user feedback information with tutor via tutor id
        /// </summary>
        /// <param name="tutorId">Tutor ID</param>
        /// <returns>user feedback information</returns>
        [Authorize(Roles = "Student")]
        [HttpGet("Feedback/tutor/{tutorId}/user")]
        public async Task<IActionResult> GetFeedbackByUser(int tutorId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var feedback = await _feedbackService.GetFeedbackByUserAsync(tutorId, userId);

                if (feedback == null)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Không đánh giá nào được tìm thấy." });

                return Ok(new Response<FeedbackDTO> { Succeeded = true, Data = feedback });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = "Lỗi hệ thống", Errors = new[] { ex.Message } });
            }
        }

    }
}
