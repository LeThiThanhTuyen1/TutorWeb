using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IContractRepository _contractRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IAuthRepository _authRepo;
        private readonly IScheduleRepository _scheduleRepo;
        private readonly ITutorRepository _tutorRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(
            IEnrollmentRepository enrollmentRepo,
            IContractRepository contractRepo,
            IAuthRepository authRepo,
            ICourseRepository courseRepo,
            ITutorRepository tutorRepo,
            IScheduleRepository scheduleRepo,
            INotificationRepository notificationRepo,
            IMemoryCache cache,
            ILogger<EnrollmentService> logger)
        {
            _enrollmentRepo = enrollmentRepo;
            _contractRepo = contractRepo;
            _courseRepo = courseRepo;
            _authRepo = authRepo;
            _tutorRepo = tutorRepo;
            _scheduleRepo = scheduleRepo;
            _notificationRepo = notificationRepo;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Response<string>> RegisterStudentForCourse(int courseId, int userId)
        {
            try
            {
                _logger.LogInformation($"Attempting to enroll userId: {userId} in CourseId: {courseId}");

                var student = await _authRepo.GetStudentByUserIdAsync(userId);
                if (student == null)
                    return new Response<string>("Học viên không tồn tại.")
                    {
                        Succeeded = false,
                        Errors = new[] { "Student Not Found" }
                    };

                int studentId = student.Id;

                bool isEligible = await _enrollmentRepo.IsStudentEligibleAsync(studentId, courseId);
                if (!isEligible)
                    return new Response<string>("Bạn đã đăng ký khóa học này trước đó.")
                    {
                        Succeeded = false,
                        Errors = new[] { "Duplicate Enrollment" }
                    };

                var course = await _courseRepo.GetCourseByIdAsync(courseId);
                if (course == null)
                    return new Response<string>("Khóa học không tồn tại.")
                    {
                        Succeeded = false,
                        Errors = new[] { "Course Not Found" }
                    };

                int enrolledCount = await _enrollmentRepo.GetEnrolledStudentCountAsync(courseId);
                int maxStudents = await _enrollmentRepo.GetMaxStudentsAsync(courseId);

                if (enrolledCount >= maxStudents)
                    return new Response<string>("Khóa học đã đủ.")
                    {
                        Succeeded = false,
                        Errors = new[] { "Course Full" }
                    };

                var newCourseSchedules = await _cache.GetOrCreateAsync(
                    $"Course_Schedules_{courseId}",
                    entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                        return _scheduleRepo.GetSchedulesByCourseIdAsync(courseId);
                    });

                var enrolledCourses = await _enrollmentRepo.GetStudentEnrolledCoursesAsync(studentId);
                var enrolledSchedules = new List<Schedule>();

                foreach (var enrolledCourse in enrolledCourses)
                {
                    var schedules = await _scheduleRepo.GetSchedulesByCourseIdAsync(enrolledCourse.Id);
                    enrolledSchedules.AddRange(schedules);
                }

                bool hasScheduleConflict = newCourseSchedules.Any(newSchedule =>
                    enrolledSchedules.Any(existingSchedule =>
                        existingSchedule.DayOfWeek == newSchedule.DayOfWeek &&
                        existingSchedule.StartHour < newSchedule.EndHour &&
                        existingSchedule.EndHour > newSchedule.StartHour
                    ));

                if (hasScheduleConflict)
                    return new Response<string>("Lịch học bị trùng so với lịch học bạn đã đăng ký!")
                    {
                        Succeeded = false,
                        Errors = new[] { "Schedule Conflict" }
                    };

                var enrollment = new Enrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    Status = "pending",
                    EnrolledAt = DateTime.UtcNow
                };

                await _enrollmentRepo.EnrollStudentAsync(enrollment);

                var contract = new Contract
                {
                    TutorId = course.TutorId,
                    StudentId = studentId,
                    CourseId = courseId,
                    Terms = "Standard Terms of Tutoring System.",
                    Fee = course.Fee,
                    StartDate = DateTime.UtcNow,
                    EndDate = course.EndDate,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                await _contractRepo.CreateContractAsync(contract);

                var UserIdByTutorId = await _authRepo.GetUserIdByTutorId(course.TutorId);

                await SendNotification(UserIdByTutorId, $"Một học sinh mới vừa đăng ký khóa học {course.CourseName} của bạn.");
                await SendNotification(userId, $"Khóa học {course.CourseName} đã được đăng ký thành công. Hợp đồng đã được tạo.");

                _logger.LogInformation($"Học sinh với ID: {studentId} đăng ký khóa học thành công với ID: {courseId}");

                return new Response<string>("Đăng ký khóa học thành công. Hợp đồng đã được tạo.")
                {
                    Succeeded = true,
                    Errors = new[] { "Đăng ký khóa học thành công." }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Có lỗi xảy ra trong quá trình đăng ký.");
                return new Response<string>("Có lỗi xảy ra trong quá trình đăng ký.")
                {
                    Succeeded = false,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<bool> UnenrollStudentAsync(int userId, int courseId)
        {
            return await _enrollmentRepo.UnenrollStudentAsync(userId, courseId);
        }

        public async Task<EnrollmentDTO> GetEnrollmentById(int enrollmentId)
        {
            return await _enrollmentRepo.GetEnrollmentByIdAsync(enrollmentId);
        }

        public async Task<List<Course>> GetStudentCourses(int studentId)
        {
            var courses = await _enrollmentRepo.GetStudentEnrolledCoursesAsync(studentId);
            return courses;
        }

        public async Task SendNotification(int userId, string message)
        {
            await _notificationRepo.CreateNotification(userId, message, "schedule_reminder");
        }
    }
}
