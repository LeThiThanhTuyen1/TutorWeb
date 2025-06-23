using TutorWebAPI.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Repositories
{
    public interface IAdminRepository
    {
        Task<int> GetTotalTutorsAsync();
        Task<int> GetTotalStudentsAsync();
        Task<int> GetTotalCoursesAsync();
        Task<int> GetTotalSchedulesAsync();
        Task<int> GetPendingEnrollmentsAsync();
        Task<int> CalculateChangeAsync(string entityType, DateTime lastMonthStart, DateTime thisMonthStart);
        Task<List<CourseStatus>> GetCourseStatusesAsync();
        Task<List<MonthlyActivity>> GetMonthlyActivitiesAsync(DateTime sixMonthsAgo);
        Task<List<Enrollment>> GetRecentEnrollmentsAsync(int take);
        Task<PagedResponse<List<User>>> GetAllUsersAsync(PaginationFilter filter, string route);
        Task<int> DeleteUsersAsync(List<int> userIds);
        Task<User> AddUserAsync(User userInfo);
        Task<User> UpdateUserAsync(int userId, User userInfo);
    }

    public class CourseStatus
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyActivity
    {
        public string Month { get; set; }
        public int NewTutors { get; set; }
        public int NewStudents { get; set; }
        public int NewCourses { get; set; }
    }
}