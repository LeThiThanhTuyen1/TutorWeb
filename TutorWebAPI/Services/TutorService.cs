using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Services
{
    public class TutorService : ITutorService
    {
        private readonly ITutorRepository _tutorRepository;
        private readonly IMemoryCache _cache;

        public TutorService(ITutorRepository tutorRepository, IMemoryCache cache)
        {
            _tutorRepository = tutorRepository;
            _cache = cache;
        }

        public async Task<PagedResponse<List<TutorDTO>>> SearchTutors(TutorSearchDTO searchCriteria, PaginationFilter filter, string route)
        {
            string cacheKey = $"SearchTutors_{searchCriteria.Subjects}_{searchCriteria.TeachingMode}_{searchCriteria.MinRating}_{searchCriteria.MaxFee}_{searchCriteria.Location}_{filter.PageNumber}_{filter.PageSize}";

            if (_cache.TryGetValue(cacheKey, out PagedResponse<List<TutorDTO>> cachedResult))
            {
                return cachedResult;
            }

            var result = await _tutorRepository.SearchTutors(searchCriteria, filter, route);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        public async Task<PagedResponse<List<TutorDTO>>> GetAllTutorsAsync(PaginationFilter filter, string route)
        {
            string cacheKey = $"AllTutors_{filter.PageNumber}_{filter.PageSize}";

            if (_cache.TryGetValue(cacheKey, out PagedResponse<List<TutorDTO>> cachedResult))
            {
                return cachedResult;
            }

            var result = await _tutorRepository.GetAllTutors(filter, route);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        public async Task<TutorDTO?> GetTutorByUserIdAsync(int userId)
        {
            string cacheKey = $"TutorByUserId_{userId}";

            if (_cache.TryGetValue(cacheKey, out TutorDTO cachedTutor))
            {
                return cachedTutor;
            }

            var tutor = await _tutorRepository.GetTutorByUserIdAsync(userId);

            if (tutor != null)
            {
                _cache.Set(cacheKey, tutor, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return tutor;
        }

        public async Task<object> GetDashboardDataAsync(int userId)
        {
            try
            {
                var stats = await _tutorRepository.GetStatsAsync(userId);
                var barData = await _tutorRepository.GetBarDataAsync(userId);
                var upcomingClasses = await _tutorRepository.GetUpcomingClassesAsync(userId);
                var recentContracts = await _tutorRepository.GetRecentContractsAsync(userId);

                return new
                {
                    stats,
                    barData,
                    upcomingClasses,
                    recentContracts
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching dashboard data", ex);
            }
        }


        public async Task<bool> DeleteTutorsAsync(List<int> tutorIds)
        {
            bool result = await _tutorRepository.DeleteTutorsAsync(tutorIds);

            if (result)
            {
                _cache.Remove("AllTutors");
                foreach (var id in tutorIds)
                {
                    _cache.Remove($"TutorByUserId_{id}");
                }
            }

            return result;
        }

        public async Task<TutorDTO?> GetTutorByIdAsync(int tutorId)
        {
            string cacheKey = $"TutorById_{tutorId}";

            if (_cache.TryGetValue(cacheKey, out TutorDTO cachedTutor))
            {
                return cachedTutor;
            }

            var tutor = await _tutorRepository.GetTutorByIdAsync(tutorId);

            if (tutor != null)
            {
                _cache.Set(cacheKey, tutor, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return tutor;
        }

        public void InvalidateTutorCache(int tutorId)
        {
            string cacheKey = $"TutorById_{tutorId}";
            _cache.Remove(cacheKey);
            _cache.Remove($"AllTutors");
        }
    }
}
