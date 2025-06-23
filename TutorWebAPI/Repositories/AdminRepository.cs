using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Helper;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminRepository> _logger;
        private readonly IUriRepository _uriRepository;

        public AdminRepository(ApplicationDbContext context, ILogger<AdminRepository> logger, IUriRepository uriRepository)
        {
            _context = context;
            _logger = logger;
            _uriRepository = uriRepository;
        }

        public async Task<int> GetTotalTutorsAsync() => await _context.Tutors.CountAsync();
        public async Task<int> GetTotalStudentsAsync() => await _context.Students.CountAsync();
        public async Task<int> GetTotalCoursesAsync() => await _context.Courses.CountAsync();
        public async Task<int> GetTotalSchedulesAsync() => await _context.Schedules.CountAsync();
        public async Task<int> GetPendingEnrollmentsAsync() => await _context.Enrollments.CountAsync(e => e.Status == "pending");

        public async Task<int> CalculateChangeAsync(string entityType, DateTime lastMonthStart, DateTime thisMonthStart)
        {
            int lastMonthCount = 0;
            int thisMonthCount = 0;

            if (entityType == "course")
            {
                lastMonthCount = await _context.Courses
                    .CountAsync(c => c.StartDate >= lastMonthStart && c.StartDate < thisMonthStart);
                thisMonthCount = await _context.Courses
                    .CountAsync(c => c.StartDate >= thisMonthStart);
            }
            else
            {
                lastMonthCount = await _context.Users
                    .CountAsync(u => u.Role == (entityType == "tutor" ? "Tutor" : "Student") &&
                                    u.CreatedAt >= lastMonthStart && u.CreatedAt < thisMonthStart);
                thisMonthCount = await _context.Users
                    .CountAsync(u => u.Role == (entityType == "tutor" ? "Tutor" : "Student") &&
                                    u.CreatedAt >= thisMonthStart);
            }

            return thisMonthCount - lastMonthCount;
        }

        public async Task<List<CourseStatus>> GetCourseStatusesAsync()
        {
            return await _context.Courses
                .GroupBy(c => c.Status)
                .Select(g => new CourseStatus
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }

        public async Task<List<MonthlyActivity>> GetMonthlyActivitiesAsync(DateTime sixMonthsAgo)
        {
            var userGroups = await _context.Users
                .Where(u => u.CreatedAt >= sixMonthsAgo)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    NewTutors = g.Count(u => u.Role == "Tutor"),
                    NewStudents = g.Count(u => u.Role == "Student")
                })
                .ToListAsync();

            var courseGroups = await _context.Courses
                .Where(c => c.CreatedAt >= sixMonthsAgo)
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    NewCourses = g.Count()
                })
                .ToListAsync();

            var monthlyActivities = userGroups
                .GroupJoin(courseGroups,
                    ug => new { ug.Year, ug.Month },
                    cg => new { cg.Year, cg.Month },
                    (ug, cg) => new MonthlyActivity
                    {
                        Month = $"{ug.Year}-{ug.Month:00}",
                        NewTutors = ug.NewTutors,
                        NewStudents = ug.NewStudents,
                        NewCourses = cg.FirstOrDefault()?.NewCourses ?? 0
                    })
                .OrderBy(m => m.Month)
                .ToList();

            var missingMonths = courseGroups
                .Where(cg => !userGroups.Any(ug => ug.Year == cg.Year && ug.Month == cg.Month))
                .Select(cg => new MonthlyActivity
                {
                    Month = $"{cg.Year}-{cg.Month:00}",
                    NewTutors = 0,
                    NewStudents = 0,
                    NewCourses = cg.NewCourses
                });

            monthlyActivities.AddRange(missingMonths);
            return monthlyActivities.OrderBy(m => m.Month).ToList();
        }

        public async Task<List<Enrollment>> GetRecentEnrollmentsAsync(int take)
        {
            return await _context.Enrollments
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Include(e => e.Course)
                .OrderByDescending(e => e.EnrolledAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<PagedResponse<List<User>>> GetAllUsersAsync(PaginationFilter filter, string route)
        {
            try
            {
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
                var query = _context.Users
                    .Where(u => u.isDeleted == false)
                    .OrderBy(u => u.Email)
                    .Select(u => new User
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Phone = u.Phone,
                        ProfileImage = u.ProfileImage,
                        Location = u.Location,
                        Role = u.Role,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth,
                        School = u.School
                    });

                return await query.ToPagedResponseAsync(validFilter, _uriRepository, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách người dùng phân trang");
                throw;
            }
        }

        public async Task<int> DeleteUsersAsync(List<int> userIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id) && u.isDeleted == false)
                    .ToListAsync();

                if (users.Count == 0)
                {
                    _logger.LogInformation("Không tìm thấy người dùng hợp lệ để xóa với ID: {UserIds}", userIds);
                    return 0;
                }

                foreach (var user in users)
                {
                    user.isDeleted = true;
                }

                int affectedRows = await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Đã xóa mềm {Count} người dùng với ID: {UserIds}", affectedRows, userIds);
                return affectedRows;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa mềm người dùng với ID: {UserIds}", userIds);
                throw;
            }
        }

        public async Task<User> AddUserAsync(User userInfo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Name = userInfo.Name,
                    Email = userInfo.Email,
                    Phone = userInfo.Phone,
                    Password = BCrypt.Net.BCrypt.HashPassword(userInfo.Phone),
                    ProfileImage = userInfo.ProfileImage,
                    DateOfBirth = userInfo.DateOfBirth,
                    School = userInfo.School,
                    Location = userInfo.Location,
                    Gender = userInfo.Gender,
                    Role = userInfo.Role,
                    CreatedAt = DateTime.Now,
                    isDeleted = false,
                    Verified = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (userInfo.Role == "Student")
                {
                    var student = new Student
                    {
                        UserId = user.Id,
                        Class = 0,
                    };
                    _context.Students.Add(student);
                }
                else if (userInfo.Role == "Tutor")
                {
                    var tutor = new Tutor
                    {
                        UserId = user.Id,
                        Experience = 0,
                        Subjects = "",
                        Introduction = "",
                        Rating = 0
                    };
                    _context.Tutors.Add(tutor);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Thêm mới người dùng thành công với ID: {UserId}", user.Id);
                return user;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi thêm mới người dùng với email: {Email}", userInfo.Email);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(int userId, User userInfo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.isDeleted);

                if (user == null)
                {
                    _logger.LogInformation("Không tìm thấy người dùng với ID: {UserId}", userId);
                    return null;
                }

                // Update basic user information
                user.Name = userInfo.Name;
                user.Email = userInfo.Email;
                user.Phone = userInfo.Phone;
                user.ProfileImage = userInfo.ProfileImage ?? "uploads/userprofile.jpg";
                user.DateOfBirth = userInfo.DateOfBirth;
                user.School = userInfo.School;
                user.Location = userInfo.Location;
                user.Gender = userInfo.Gender;
                user.Role = userInfo.Role;

                if (!string.IsNullOrEmpty(userInfo.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(userInfo.Password);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Cập nhật người dùng thành công với ID: {UserId}", userId);
                return user;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật người dùng với ID: {UserId}", userId);
                throw;
            }
        }
    }
}