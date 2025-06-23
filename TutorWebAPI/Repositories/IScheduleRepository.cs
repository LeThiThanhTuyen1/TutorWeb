using System.Collections.Generic;
using System.Threading.Tasks;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public interface IScheduleRepository
    {
        Task<IEnumerable<Schedule>> GetAllSchedulesAsync();
        Task<Schedule?> GetScheduleByIdAsync(int id);
        Task<IEnumerable<Schedule>> GetSchedulesByCourseIdAsync(int courseId);
        Task AddScheduleAsync(Schedule schedule);
        Task UpdateScheduleAsync(Schedule schedule);
        Task DeleteSchedulesAsync(List<int> scheduleIds);
        Task<bool> ScheduleExistsAsync(int id);
        Task<bool> CheckScheduleConflictAsync(int tutorId, int dayOfWeek, TimeSpan startHour, TimeSpan endHour, int? excludeScheduleId = null);
        //Task<string> GetScheduleStatusAsync(int id);
    }
}
