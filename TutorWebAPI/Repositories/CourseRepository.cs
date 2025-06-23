using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Helper;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriRepository _uriRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<CourseRepository> _logger;

        public CourseRepository(ApplicationDbContext context, IUriRepository uriRepository, ILogger<CourseRepository> logger, INotificationRepository notificationRepository)
        {
            _context = context;
            _uriRepository = uriRepository;
            _logger = logger;
            _notificationRepository = notificationRepository;
        }

        public async Task<bool> CourseExistsAsync(int courseId)
        {
            try
            {
                return await _context.Courses.AnyAsync(c => c.Id == courseId && c.isDeleted == false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra sự tồn tại của khóa học với ID {CourseId}", courseId);
                throw new Exception("Đã xảy ra lỗi khi kiểm tra khóa học. Vui lòng thử lại sau.");
            }
        }

        public async Task<PagedResponse<List<CourseDTO>>> GetAllCoursesAsync(PaginationFilter filter, string route)
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.isDeleted == false) 
                    .Include(c => c.Tutor)
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CourseDTO
                    {
                        Id = c.Id,
                        CourseName = c.CourseName,
                        Description = c.Description,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Subject = c.Subject,
                        Fee = c.Fee,
                        MaxStudents = c.MaxStudents,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt,
                        TutorName = c.Tutor != null ? c.Tutor.User.Name : "Không có thông tin"
                    })
                    .ToPagedResponseAsync(filter, _uriRepository, route);

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tất cả khóa học.");
                throw new Exception("Đã xảy ra lỗi khi lấy danh sách khóa học. Vui lòng thử lại sau.");
            }
        }

        public async Task<PagedResponse<List<CourseDTO>>> GetAllCoursesAsync(
            PaginationFilter filter,
            string route,
            string? searchTerm = null,
            IEnumerable<string>? statuses = null)
        {
            try
            {
                if (filter == null || filter.PageNumber < 1 || filter.PageSize < 1)
                {
                    _logger.LogWarning("Tham số phân trang không hợp lệ: {Filter}", filter);
                    throw new ArgumentException("Tham số phân trang không hợp lệ.");
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().Replace("%", "[%]").Replace("_", "[_]");
                }

                var validStatuses = new HashSet<string> { "ongoing", "completed", "canceled", "pending", "coming" };
                if (statuses != null && statuses.Any(s => !validStatuses.Contains(s)))
                {
                    _logger.LogWarning("Trạng thái không hợp lệ: {Statuses}", string.Join(",", statuses));
                    throw new ArgumentException("Một hoặc nhiều trạng thái không hợp lệ.");
                }

                var query = _context.Courses
                    .Where(c => c.isDeleted == false) 
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.CourseName ?? "", $"%{searchTerm}%") ||
                        EF.Functions.Like(c.Description ?? "", $"%{searchTerm}%") ||
                        EF.Functions.Like(c.Tutor != null && c.Tutor.User != null ? c.Tutor.User.Name ?? "" : "", $"%{searchTerm}%"));
                }

                if (statuses != null && statuses.Any())
                {
                    query = query.Where(c => c.Status != null && statuses.Contains(c.Status.ToLower()));
                }

                _logger.LogInformation(
                    "Thực hiện truy vấn khóa học với searchTerm: {SearchTerm}, statuses: {Statuses}, pageNumber: {PageNumber}, pageSize: {PageSize}",
                    searchTerm ?? "không có", statuses != null ? string.Join(",", statuses) : "không có", filter.PageNumber, filter.PageSize);

                var courses = await query
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CourseDTO
                    {
                        Id = c.Id,
                        CourseName = c.CourseName ?? "",
                        Description = c.Description ?? "",
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Fee = c.Fee,
                        Subject = c.Subject,
                        MaxStudents = c.MaxStudents,
                        Status = c.Status ?? "",
                        CreatedAt = c.CreatedAt,
                        TutorName = c.Tutor != null && c.Tutor.User != null ? c.Tutor.User.Name : "Không có thông tin"
                    })
                    .ToPagedResponseAsync(filter, _uriRepository, route);

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi lấy danh sách khóa học. SearchTerm: {SearchTerm}, Statuses: {Statuses}, PageNumber: {PageNumber}, PageSize: {PageSize}, InnerException: {InnerException}",
                    searchTerm ?? "không có", statuses != null ? string.Join(",", statuses) : "không có", filter.PageNumber, filter.PageSize, ex.InnerException?.Message);
                throw new Exception($"Lỗi khi lấy danh sách khóa học: {ex.Message}", ex);
            }
        }

        public async Task<PagedResponse<List<CourseDTO>>> GetTutorCoursesByUserId(PaginationFilter filter, string route, int userId)
        {
            try
            {
                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return new PagedResponse<List<CourseDTO>>(null, filter.PageNumber, filter.PageSize)
                    {
                        Succeeded = false,
                        Message = $"Người dùng với ID {userId} không phải là giảng viên hoặc không có khóa học."
                    };
                }

                var courses = await _context.Courses
                    .Where(c => c.isDeleted == false && c.TutorId == tutor.Id) 
                    .Include(c => c.Tutor)
                    .Include(c => c.Schedules)
                    .Select(c => new CourseDTO
                    {
                        Id = c.Id,
                        CourseName = c.CourseName,
                        Description = c.Description,
                        Fee = c.Fee,
                        CreatedAt = c.CreatedAt,
                        MaxStudents = c.MaxStudents,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Subject = c.Subject,
                        Status = c.Status,
                        TutorId = c.Tutor.Id,
                        TutorName = c.Tutor.User.Name,
                        Schedule = c.Schedules.Select(s => new ScheduleDTO
                        {
                            Id = s.Id,
                            StartHour = s.StartHour,
                            EndHour = s.EndHour,
                            Location = s.Location,
                            DayOfWeek = s.DayOfWeek,
                            TutorId = s.TutorId,
                            CourseId = s.CourseId,
                            Mode = s.Mode
                        }).ToList()
                    })
                    .ToPagedResponseAsync(filter, _uriRepository, route);

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách khóa học cho người dùng ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<PagedResponse<List<EnrollmentDTO>>> GetStudentCoursesByUserId(PaginationFilter filter, string route, int userId)
        {
            try
            {
                var student = await _context.Students
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (student == null)
                {
                    return new PagedResponse<List<EnrollmentDTO>>(null, filter.PageNumber, filter.PageSize)
                    {
                        Succeeded = false,
                        Message = $"Người dùng với ID {userId} không phải là học viên hoặc không có đăng ký."
                    };
                }

                var enrollmentsQuery = _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Schedules)
                    .Include(e => e.Course.Tutor)
                        .ThenInclude(t => t.User)
                    .Where(e => e.Student.UserId == userId && e.Course.isDeleted == false); 

                var totalRecords = await enrollmentsQuery.CountAsync();
                if (totalRecords == 0)
                {
                    return new PagedResponse<List<EnrollmentDTO>>(null, filter.PageNumber, filter.PageSize)
                    {
                        Succeeded = false,
                        Message = $"Không tìm thấy đăng ký nào cho người dùng ID {userId}."
                    };
                }

                var courses = await enrollmentsQuery
                    .OrderBy(e => e.EnrolledAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(e => new EnrollmentDTO
                    {
                        Id = e.Id,
                        CourseId = e.CourseId,
                        TutorName = e.Course.Tutor.User.Name,
                        CourseName = e.Course.CourseName,
                        Subject = e.Course.Subject,
                        StartDate = e.Course.StartDate,
                        EndDate = e.Course.EndDate,
                        Fee = e.Course.Fee,
                        Status = e.Status,
                        EnrolledAt = e.EnrolledAt,
                        Schedule = e.Course.Schedules.Select(s => new ScheduleDTO
                        {
                            Id = s.Id,
                            DayOfWeek = s.DayOfWeek,
                            StartHour = s.StartHour,
                            EndHour = s.EndHour,
                            Mode = s.Mode,
                            Location = s.Location,
                            Status = s.Status
                        }).ToList()
                    })
                    .ToPagedResponseAsync(filter, _uriRepository, route);

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách khóa học cho người dùng ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<CourseDTO?> GetCourseByIdAsync(int id)
        {
            try
            {
                var course = await _context.Courses
                    .Where(c => c.isDeleted == false) 
                        .Include(t => t.Tutor)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Schedules)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (course == null)
                    return null;

                var schedules = course.Schedules?.Select(s => new ScheduleDTO
                {
                    Id = s.Id,
                    CourseId = s.CourseId,
                    TutorId = s.TutorId,
                    DayOfWeek = s.DayOfWeek,
                    StartHour = s.StartHour,
                    EndHour = s.EndHour,
                    Mode = s.Mode,
                    Location = s.Location,
                    Status = s.Status
                }).ToList() ?? new List<ScheduleDTO>();

                var courseDto = new CourseDTO
                {
                    Id = course.Id,
                    TutorId = course.TutorId,
                    CourseName = course.CourseName,
                    Description = course.Description,
                    StartDate = course.StartDate,
                    Subject = course.Subject,
                    EndDate = course.EndDate,
                    Fee = course.Fee,
                    MaxStudents = course.MaxStudents,
                    Status = course.Status,
                    CreatedAt = course.CreatedAt,
                    TutorName = course.Tutor?.User?.Name,
                    Schedule = schedules
                };

                return courseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin khóa học với ID {CourseId}", id);
                throw new Exception("Đã xảy ra lỗi khi lấy thông tin khóa học. Vui lòng thử lại sau.");
            }
        }

        public async Task<CourseDTO> AddCourseAsync(int userId, CourseDTO courseDto)
        {
            var tutor = await _context.Tutors
                .Where(t => t.UserId == userId)
                .FirstOrDefaultAsync();

            if (tutor == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giảng viên cho người dùng này.");
            }

            var existingCourse = await _context.Courses
                .Where(c => c.isDeleted == false && c.CourseName == courseDto.CourseName) 
                .FirstOrDefaultAsync();

            if (existingCourse != null)
            {
                throw new InvalidOperationException("Khóa học với tên này đã tồn tại.");
            }

            var course = new Course
            {
                TutorId = tutor.Id,
                CourseName = courseDto.CourseName,
                Description = courseDto.Description,
                MaxStudents = courseDto.MaxStudents,
                Fee = courseDto.Fee,
                StartDate = courseDto.StartDate,
                EndDate = courseDto.EndDate,
                Subject = courseDto.Subject,
                Status = "coming",
                isDeleted = false 
            };

            await _context.Courses.AddAsync(course);
            await _context.SaveChangesAsync();

            return new CourseDTO
            {
                Id = course.Id,
                CourseName = course.CourseName,
                Description = course.Description,
                TutorId = course.TutorId,
                Fee = course.Fee,
                MaxStudents = course.MaxStudents,
                Status = course.Status
            };
        }

        public async Task<Course> GetCourseById(int id)
        {
            try
            {
                var course = await _context.Courses
                    .Where(c => c.isDeleted == false) 
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (course == null)
                    return null;

                return course;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin khóa học với ID {CourseId}", id);
                throw new Exception("Đã xảy ra lỗi khi lấy thông tin khóa học. Vui lòng thử lại sau.");
            }
        }

        public async Task<CourseDTO> UpdateCourseAsync(Course course)
        {
            try
            {
                if (course.isDeleted)
                {
                    throw new InvalidOperationException("Không thể cập nhật khóa học đã bị xóa.");
                }

                _context.Courses.Update(course);
                await _context.SaveChangesAsync();

                var courseDto = new CourseDTO
                {
                    Id = course.Id,
                    CourseName = course.CourseName,
                    Description = course.Description,
                    TutorId = course.TutorId,
                    MaxStudents = course.MaxStudents,
                    Fee = course.Fee,
                    StartDate = course.StartDate,
                    EndDate = course.EndDate,
                    Status = course.Status,
                    CreatedAt = course.CreatedAt
                };

                return courseDto;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi cập nhật khóa học với ID {CourseId}", course.Id);
                throw new Exception("Đã xảy ra lỗi khi cập nhật khóa học. Vui lòng thử lại sau.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi cập nhật khóa học.");
                throw new Exception("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }
        }

        public async Task DeleteCoursesAsync(List<int> courseIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var courses = await _context.Courses
                    .Where(c => courseIds.Contains(c.Id) && c.isDeleted == false) 
                    .Include(c => c.Enrollments)
                    .ToListAsync();

                var deletableCourses = courses.Where(c => c.Status != "ongoing").ToList();

                if (!deletableCourses.Any())
                {
                    _logger.LogWarning("Không có khóa học nào có thể xóa. Các khóa học đang diễn ra hoặc không hợp lệ: {Ids}", string.Join(", ", courseIds));
                    throw new Exception("Không có khóa học nào có thể xóa. Các khóa học đang diễn ra hoặc không hợp lệ.");
                }

                foreach (var course in deletableCourses)
                {
                    course.isDeleted = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Đã xóa mềm các khóa học: {Ids}", string.Join(", ", courseIds));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi xóa mềm các khóa học: {Ids}. Lỗi chi tiết: {InnerException}",
                    string.Join(", ", courseIds), ex.InnerException?.Message);
                throw new Exception("Đã xảy ra lỗi cơ sở dữ liệu khi xóa các khóa học.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi không mong muốn khi xóa mềm các khóa học: {Ids}. Lỗi chi tiết: {InnerException}",
                    string.Join(", ", courseIds), ex.InnerException?.Message);
                throw new Exception("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }
        }

        public async Task<List<StudentDTO>> GetEnrolledStudentsByCourseIdAsync(int courseId)
        {
            try
            {
                return await _context.Enrollments
                    .Where(e => e.CourseId == courseId && e.Course.isDeleted == false)
                    .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                    .Select(e => new StudentDTO
                    {
                        Id = e.Student.Id,
                        UserId = e.Student.UserId,
                        Status = e.Status,
                        EnrolledAt = e.EnrolledAt,
                        ProfileImage = e.Student.User.ProfileImage,
                        Phone = e.Student.User != null ? e.Student.User.Phone : "Chưa có thông tin",
                        FullName = e.Student.User != null ? e.Student.User.Name : "Chưa có thông tin",
                        Email = e.Student.User != null ? e.Student.User.Email : "Chưa có thông tin"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách học viên đã đăng ký cho khóa học ID {CourseId}", courseId);
                throw new Exception("Đã xảy ra lỗi khi lấy danh sách học viên. Vui lòng thử lại sau.");
            }
        }

        public async Task<int> GetCourseCountAsync()
        {
            try
            {
                return await _context.Courses
                    .Where(c => c.isDeleted == false) 
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đếm số lượng khóa học.");
                throw new Exception("Đã xảy ra lỗi khi đếm số lượng khóa học. Vui lòng thử lại sau.");
            }
        }
    }
}