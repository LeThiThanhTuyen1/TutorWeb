using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardResponse> GetDashboardDataAsync();
        Task<Response<User>> AddUserAsync(User userInfo); 
        Task<Response<User>> UpdateUserAsync(int userId, User userInfo); 
        Task<PagedResponse<List<User>>> GetPagedUsersAsync(PaginationFilter filter, string route);
        Task<Response<string>> DeleteUsersAsync(List<int> userIds);
    }
}