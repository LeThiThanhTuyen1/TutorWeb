using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public interface ITutorRepository
    {
        Task<PagedResponse<List<TutorDTO>>> SearchTutors(TutorSearchDTO searchCriteria, PaginationFilter filter, string route);
        Task<bool> TutorExistsAsync(int tutorId);
        Task<PagedResponse<List<TutorDTO>>> GetAllTutors(PaginationFilter filter, string route);
        Task<TutorDTO?> GetTutorByUserIdAsync(int userId);
        Task<object> GetStatsAsync(int userId);
        Task<List<object>> GetBarDataAsync(int userId);
        Task<List<object>> GetUpcomingClassesAsync(int userId);
        Task<List<object>> GetRecentContractsAsync(int userId);
        Task<TutorDTO?> GetTutorByIdAsync(int tutorId);
        Task<bool> DeleteTutorsAsync(List<int> tutorIds);
        Task<int> GetTutorCountAsync();
        Task<List<Tutor>> GetTutorsBySubjectAsync(string subject);
    }
}
