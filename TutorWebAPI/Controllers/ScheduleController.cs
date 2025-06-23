using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(IScheduleService scheduleService, ILogger<ScheduleController> logger)
        {
            _scheduleService = scheduleService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all schedules.
        /// </summary>
        [HttpGet("Schedule")]
        public async Task<ActionResult<Response<IEnumerable<Schedule>>>> GetAllSchedules()
        {
            try
            {
                var schedules = await _scheduleService.GetAllSchedulesAsync();
                return Ok(new Response<IEnumerable<Schedule>>
                {
                    Succeeded = false,
                    Message = "Lỗi khi lấy danh sách lịch học.",
                    Errors = null,
                    Data = schedules
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi khi lấy danh sách lịch học: {Message}", ex.Message);
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi khi lấy danh sách lịch học.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Retrieves a schedule by its ID.
        /// </summary>
        [HttpGet("Schedule/{id}")]
        public async Task<ActionResult<Response<Schedule>>> GetScheduleById(int id)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                if (schedule == null)
                {
                    _logger.LogWarning("Lịch học với ID {Id} không tồn tại.", id);
                    return NotFound(new Response<string>
                    {
                        Succeeded = false,
                        Message = "Lịch học không tồn tại.",
                        Errors = null
                    });
                }
                return Ok(new Response<Schedule>(schedule));
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi khi lấy lịch học với ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi khi lấy thông tin lịch học.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Creates a new schedule.
        /// </summary>
        [Authorize(Roles = "Tutor,Admin")]
        [HttpPost("Schedule")]
        public async Task<ActionResult<Response<Schedule>>> CreateSchedule([FromBody] ScheduleDTO scheduleDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray()
                });

            try
            {
                var schedule = new Schedule
                {
                    TutorId = scheduleDto.TutorId,
                    CourseId = scheduleDto.CourseId,
                    DayOfWeek = scheduleDto.DayOfWeek,
                    StartHour = scheduleDto.StartHour,
                    EndHour = scheduleDto.EndHour,
                    Mode = scheduleDto.Mode,
                    Location = scheduleDto.Location,
                    Status = "scheduled"
                };

                await _scheduleService.AddScheduleAsync(schedule);

                return CreatedAtAction(nameof(GetScheduleById), new { id = schedule.Id }, new Response<Schedule>
                {
                    Data = schedule,
                    Succeeded = true,
                    Message = "Tạo lịch học thành công."
                });
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning("Lỗi khi tạo mới lịch học: {Message}", ex.Message);
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi không mong muốn xảy ra khi tạo lịch học: {Message}", ex.Message);
                return StatusCode(500, new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi không mong muốn xảy ra khi tạo lịch học."
                });
            }
        }

        /// <summary>
        /// Updates an existing schedule by its ID.
        /// </summary>
        [Authorize(Roles = "Tutor,Admin")]
        [HttpPut("Schedule/{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleDTO scheduleDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray()
                });

            try
            {
                var existingSchedule = await _scheduleService.GetScheduleByIdAsync(id);
                if (existingSchedule == null)
                    return NotFound(new Response<string>
                    {
                        Succeeded = false,
                        Message = "Lịch học không tồn tại.",
                        Errors = null
                    });

                existingSchedule.TutorId = scheduleDto.TutorId;
                existingSchedule.CourseId = scheduleDto.CourseId;
                existingSchedule.DayOfWeek = scheduleDto.DayOfWeek;
                existingSchedule.StartHour = scheduleDto.StartHour;
                existingSchedule.EndHour = scheduleDto.EndHour;
                existingSchedule.Mode = scheduleDto.Mode;
                existingSchedule.Location = scheduleDto.Location;

                await _scheduleService.UpdateScheduleAsync(existingSchedule);

                return Ok(new Response<ScheduleDTO>
                {
                    Succeeded = true,
                    Message = "Cập nhật lịch học thành công.",
                    Data = scheduleDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi khi cập nhật lịch học với {Id}: {Message}", id, ex.Message);
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi khi cập nhật lịch học.",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Deletes multiple schedules based on their IDs.
        /// </summary>
        [Authorize(Roles = "Tutor,Admin")]
        [HttpDelete("Schedules")]
        public async Task<IActionResult> DeleteSchedules([FromBody] List<int> scheduleIds)
        {
            if (scheduleIds == null || !scheduleIds.Any())
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Lịch học không hợp lệ.",
                    Errors = new[] { "Lịch học không thể bị xóa vì vài lý do." }
                });

            try
            {
                await _scheduleService.DeleteSchedulesAsync(scheduleIds);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi khi xóa  lịch học: {Message}", ex.Message);
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi khi xóa  lịch học.",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
