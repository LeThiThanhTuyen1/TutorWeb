
using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface IScheduleService
    {
        Task<IEnumerable<Schedule>> GetAllSchedulesAsync(); 
        Task<Schedule?> GetScheduleByIdAsync(int id); 
        Task AddScheduleAsync(Schedule schedule); 
        Task UpdateScheduleAsync(Schedule schedule); 
        Task DeleteSchedulesAsync(List<int> scheduleIds);
    }
}