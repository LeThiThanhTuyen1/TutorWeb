using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.Models;
using TutorWebAPI.Repositories;

namespace TutorWebAPI.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ScheduleService> _logger;

        public ScheduleService(IScheduleRepository scheduleRepository, IMemoryCache cache, ILogger<ScheduleService> logger)
        {
            _scheduleRepository = scheduleRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<Schedule>> GetAllSchedulesAsync()
        {
            const string cacheKey = "all_schedules";
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<Schedule> schedules))
            {
                schedules = await _scheduleRepository.GetAllSchedulesAsync();
                _cache.Set(cacheKey, schedules, TimeSpan.FromMinutes(5));
            }
            return schedules;
        }

        public async Task<Schedule?> GetScheduleByIdAsync(int id)
        {
            string cacheKey = $"schedule_{id}";
            if (!_cache.TryGetValue(cacheKey, out Schedule? schedule))
            {
                schedule = await _scheduleRepository.GetScheduleByIdAsync(id);
                if (schedule != null)
                {
                    _cache.Set(cacheKey, schedule, TimeSpan.FromMinutes(10));
                }
            }
            return schedule;
        }

        public async Task AddScheduleAsync(Schedule schedule)
        {
            try
            {
                await _scheduleRepository.AddScheduleAsync(schedule);
                _cache.Remove("all_schedules");
            }
            catch (ScheduleConflictException ex)
            {
                _logger.LogWarning("Schedule conflict: {Message}", ex.Message);
                throw new BusinessException("Lịch học bị trùng với lịch học khác.");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Error in repository while adding schedule.");
                throw new BusinessException("Có lỗi xảy ra khi thêm lịch học.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding schedule.");
                throw new BusinessException("Lỗi không mong muốn xảy ra khi thêm mới lịch học.");
            }
        }

        public async Task UpdateScheduleAsync(Schedule schedule)
        {
            _logger.LogInformation("Updating schedule {ScheduleId}", schedule.Id);
            await _scheduleRepository.UpdateScheduleAsync(schedule);
            _cache.Remove($"schedule_{schedule.Id}");
            _cache.Remove("all_schedules");
        }

        public async Task DeleteSchedulesAsync(List<int> scheduleIds)
        {
            _logger.LogWarning("Deleting schedules: {ScheduleIds}", string.Join(", ", scheduleIds));
            await _scheduleRepository.DeleteSchedulesAsync(scheduleIds);
            foreach (var id in scheduleIds)
            {
                _cache.Remove($"schedule_{id}");
            }
            _cache.Remove("all_schedules");
        }
    }
}
