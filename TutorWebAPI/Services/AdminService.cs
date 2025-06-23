using TutorWebAPI.DTOs;
using TutorWebAPI.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using Microsoft.Extensions.Caching.Memory;

namespace TutorWebAPI.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IMemoryCache _cache;

        public AdminService(IAdminRepository adminRepository, IMemoryCache cache)
        {
            _adminRepository = adminRepository;
            _cache = cache;
        }

        public async Task<AdminDashboardResponse> GetDashboardDataAsync()
        {
            try
            {
                var lastMonthStart = DateTime.Now.AddMonths(-1).Date;
                var thisMonthStart = DateTime.Now.Date;
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);

                var stats = new AdminStatsDTO
                {
                    TotalTutors = await _adminRepository.GetTotalTutorsAsync(),
                    TotalStudents = await _adminRepository.GetTotalStudentsAsync(),
                    TotalCourses = await _adminRepository.GetTotalCoursesAsync(),
                    TotalSchedules = await _adminRepository.GetTotalSchedulesAsync(),
                    PendingEnrollments = await _adminRepository.GetPendingEnrollmentsAsync(),
                    TutorsChange = await _adminRepository.CalculateChangeAsync("tutor", lastMonthStart, thisMonthStart),
                    StudentsChange = await _adminRepository.CalculateChangeAsync("student", lastMonthStart, thisMonthStart),
                    CoursesChange = await _adminRepository.CalculateChangeAsync("course", lastMonthStart, thisMonthStart)
                };

                var courseStatuses = (await _adminRepository.GetCourseStatusesAsync())
                    .Select(cs => new CourseStatusDTO
                    {
                        Status = cs.Status,
                        Count = cs.Count
                    })
                    .ToList();

                var monthlyActivities = (await _adminRepository.GetMonthlyActivitiesAsync(sixMonthsAgo))
                    .Select(ma => new MonthlyActivityDTO
                    {
                        Month = ma.Month,
                        NewTutors = ma.NewTutors,
                        NewStudents = ma.NewStudents,
                        NewCourses = ma.NewCourses
                    })
                    .ToList();

                var recentEnrollments = (await _adminRepository.GetRecentEnrollmentsAsync(10))
                    .Select(e => new RecentEnrollmentDTO
                    {
                        Id = e.Id,
                        StudentName = e.Student?.User?.Name ?? "Không có thông tin",
                        CourseName = e.Course?.CourseName ?? "Không có thông tin",
                        Status = e.Status,
                        EnrolledAt = e.EnrolledAt
                    })
                    .ToList();

                return new AdminDashboardResponse
                {
                    Stats = stats,
                    CourseStatuses = courseStatuses,
                    MonthlyActivities = monthlyActivities,
                    RecentEnrollments = recentEnrollments
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thống kê.", ex);
            }
        }

        public async Task<PagedResponse<List<User>>> GetPagedUsersAsync(PaginationFilter filter, string route)
        {
            try
            {
                var result = await _adminRepository.GetAllUsersAsync(filter, route);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy danh sách người dùng đã phân trang.", ex);
            }
        }

        public async Task<Response<string>> DeleteUsersAsync(List<int> userIds)
        {
            try
            {
                int affectedRows = await _adminRepository.DeleteUsersAsync(userIds);

                if (affectedRows == 0)
                    return new Response<string>("Lỗi khi xóa người dùng.");

                return new Response<string>($"{affectedRows} đã được xóa thành công.")
                {
                    Succeeded = true
                };
            }
            catch (Exception ex)
            {
                return new Response<string>("Lỗi khi xóa người dùng: " + ex.Message);
            }
        }

        public async Task<Response<User>> AddUserAsync(User userInfo)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(userInfo.Name) || string.IsNullOrEmpty(userInfo.Email))
                {
                    return new Response<User>("Tên và email là bắt buộc.");
                }

                // Check if email exists
                var existingUser = await _adminRepository.GetAllUsersAsync(
                    new PaginationFilter(1, 1),
                    "").ContinueWith(t => t.Result.Data.FirstOrDefault(u => u.Email == userInfo.Email));

                if (existingUser != null)
                {
                    return new Response<User>("Email đã tồn tại trong hệ thống.");
                }

                var user = await _adminRepository.AddUserAsync(userInfo);
                return new Response<User>(userInfo)
                {
                    Succeeded = true,
                    Message = "Thêm người dùng thành công."
                };
            }
            catch (Exception ex)
            {
                return new Response<User>("Lỗi khi thêm người dùng: " + ex.Message);
            }
        }

        public async Task<Response<User>> UpdateUserAsync(int userId, User userInfo)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(userInfo.Name) || string.IsNullOrEmpty(userInfo.Email))
                {
                    return new Response<User>("Tên và email là bắt buộc.");
                }

                var user = await _adminRepository.UpdateUserAsync(userId, userInfo);
                if (user == null)
                {
                    return new Response<User>("Không tìm thấy người dùng.");
                }

                return new Response<User>(userInfo)
                {
                    Succeeded = true,
                    Message = "Cập nhật người dùng thành công."
                };
            }
            catch (Exception ex)
            {
                return new Response<User>("Lỗi khi cập nhật người dùng: " + ex.Message);
            }
        }
    }
}