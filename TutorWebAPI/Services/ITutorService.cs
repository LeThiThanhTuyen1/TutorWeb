using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface ITutorService
    {
        Task<PagedResponse<List<TutorDTO>>> SearchTutors(TutorSearchDTO searchCriteria, PaginationFilter filter, string route);
        Task<PagedResponse<List<TutorDTO>>> GetAllTutorsAsync(PaginationFilter filter, string route);
        Task<TutorDTO?> GetTutorByUserIdAsync(int userId);
        Task<object> GetDashboardDataAsync(int userId);
        void InvalidateTutorCache(int tutorId);
        Task<bool> DeleteTutorsAsync(List<int> tutorIds);
        Task<TutorDTO?> GetTutorByIdAsync(int tutorId);
    }
}