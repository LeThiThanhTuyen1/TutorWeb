using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScheduleRepository> _logger;

        public ScheduleRepository(ApplicationDbContext context, ILogger<ScheduleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Schedule>> GetAllSchedulesAsync()
        {
            try
            {
                return await _context.Schedules.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all schedules.");
                throw new Exception("An error occurred while retrieving all schedules.");
            }
        }

        public async Task<Schedule?> GetScheduleByIdAsync(int id)
        {
            try
            {
                return await _context.Schedules.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving schedule by id {Id}.", id);
                throw new Exception("An error occurred while retrieving the schedule.");
            }
        }

        public async Task<IEnumerable<Schedule>> GetSchedulesByCourseIdAsync(int courseId)
        {
            try
            {
                return await _context.Schedules.Where(s => s.CourseId == courseId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving schedules for course {CourseId}.", courseId);
                throw new Exception("An error occurred while retrieving the course schedules.");
            }
        }

        public async Task AddScheduleAsync(Schedule schedule)
        {
            try
            {
                bool conflict = await CheckScheduleConflictAsync(schedule.TutorId, schedule.DayOfWeek, schedule.StartHour, schedule.EndHour);
                if (conflict)
                {
                    _logger.LogWarning("Schedule conflict detected for tutor {TutorId} on {DayOfWeek} at {StartHour}.",
                                        schedule.TutorId, schedule.DayOfWeek, schedule.StartHour);
                    throw new ScheduleConflictException("Trùng lịch học.");
                }

                await _context.Schedules.AddAsync(schedule);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added a new schedule for tutor {TutorId}.", schedule.TutorId);
            }
            catch (ScheduleConflictException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding a new schedule for tutor {TutorId}.", schedule.TutorId);
                throw new RepositoryException("Có lỗi không mong muốn xảy ra khi tạo lịch học mới.");
            }
        }

        public async Task UpdateScheduleAsync(Schedule schedule)
        {
            try
            {
                var existingSchedule = await _context.Schedules.FindAsync(schedule.Id);
                if (existingSchedule == null)
                {
                    _logger.LogWarning("Schedule {ScheduleId} not found.", schedule.Id);
                    throw new Exception("Lịch học không tồn tại.");
                }

                string status = await GetScheduleStatusAsync(schedule.Id);
                if (status == "canceled")
                {
                    _logger.LogWarning("Cannot edit schedule {ScheduleId} with canceled status.", schedule.Id);
                    throw new Exception("Khóa học đã bị hủy.");
                }

                bool conflict = await CheckScheduleConflictAsync(schedule.TutorId, schedule.DayOfWeek, schedule.StartHour, schedule.EndHour, schedule.Id);
                if (conflict)
                {
                    _logger.LogWarning("Schedule conflict detected for schedule {ScheduleId}.", schedule.Id);
                    throw new Exception("Lịch học bị trùng so với các lịch học mà bạn đã đăng ký.");
                }

                existingSchedule.DayOfWeek = schedule.DayOfWeek;
                existingSchedule.StartHour = schedule.StartHour;
                existingSchedule.EndHour = schedule.EndHour;
                existingSchedule.TutorId = schedule.TutorId;
                existingSchedule.CourseId = schedule.CourseId;
                existingSchedule.Mode = schedule.Mode;
                existingSchedule.Location = schedule.Location;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated schedule {ScheduleId}.", schedule.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating schedule {ScheduleId}.", schedule.Id);
                throw new Exception("An error occurred while updating the schedule.", ex);
            }
        }

        private async Task<string> GetScheduleStatusAsync(int scheduleId)
        {
            var schedule = await _context.Schedules
                .AsNoTracking() 
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null)
            {
                return null;
            }

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == schedule.CourseId);

            return course?.Status ?? string.Empty; 
        }

        public async Task DeleteSchedulesAsync(List<int> scheduleIds)
        {
            try
            {
                var schedules = await _context.Schedules
                    .AsNoTracking()
                    .Where(s => scheduleIds.Contains(s.Id))
                    .ToListAsync();

                if (schedules.Any())
                {
                    _context.Schedules.RemoveRange(schedules);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted schedules with ids: {ScheduleIds}.", string.Join(", ", scheduleIds));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting schedules with ids: {ScheduleIds}.", string.Join(", ", scheduleIds));
                throw new Exception("An error occurred while deleting the schedules.");
            }
        }

        public async Task<bool> ScheduleExistsAsync(int id)
        {
            try
            {
                return await _context.Schedules.AnyAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if schedule {ScheduleId} exists.", id);
                throw new Exception("An error occurred while checking the schedule existence.");
            }
        }

        public async Task<bool> CheckScheduleConflictAsync(int tutorId, int dayOfWeek, TimeSpan startHour, TimeSpan endHour, int? excludeScheduleId = null)
        {
            try
            {
                return await _context.Schedules
                    .AsNoTracking()
                    .AnyAsync(s =>
                    s.TutorId == tutorId &&
                    s.DayOfWeek == dayOfWeek &&
                    (
                        (startHour >= s.StartHour && startHour < s.EndHour) ||
                        (endHour > s.StartHour && endHour <= s.EndHour) ||
                        (startHour <= s.StartHour && endHour >= s.EndHour)
                    ) &&
                    (excludeScheduleId == null || s.Id != excludeScheduleId)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking schedule conflict for tutor {TutorId} on day {DayOfWeek}.", tutorId, dayOfWeek);
                throw new Exception("An error occurred while checking for schedule conflicts.");
            }
        }
    }
}
