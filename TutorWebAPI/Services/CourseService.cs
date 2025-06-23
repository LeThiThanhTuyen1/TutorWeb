using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;
using System.Collections.Concurrent;

namespace TutorWebAPI.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly INotificationRepository _notificationRepo;
        private readonly IMemoryCache _cache;
        private const int CacheExpirationMinutes = 10;
        private readonly ConcurrentDictionary<string, bool> _cacheKeys = new();

        public CourseService(ICourseRepository courseRepo, INotificationRepository notificationRepo, IMemoryCache cache)
        {
            _courseRepository = courseRepo;
            _notificationRepo = notificationRepo;
            _cache = cache;
        }

        public async Task<PagedResponse<List<CourseDTO>>> GetTutorCoursesByUserId(PaginationFilter filter, string route, int userId)
        {
            var courses = await _courseRepository.GetTutorCoursesByUserId(filter, route, userId);

            return courses;
        }

        public async Task<PagedResponse<List<EnrollmentDTO>>> GetStudentCoursesByUserId(PaginationFilter filter, string route, int userId)
        {
            var courses = await _courseRepository.GetStudentCoursesByUserId(filter, route, userId);

            return courses;
        }

        public async Task<List<StudentDTO>> GetStudentsByCourseIdAsync(int id)
        {
            if (!await _courseRepository.CourseExistsAsync(id))
                throw new Exception("Course does not exist.");

            var students = await _courseRepository.GetEnrolledStudentsByCourseIdAsync(id);

            return students;
        }

        public async Task<PagedResponse<List<CourseDTO>>> GetAllCoursesAsync(
            PaginationFilter filter,
            string route,
            string? searchTerm = null,
            IEnumerable<string>? statuses = null)
        {
            string statusKey = statuses != null && statuses.Any()
                ? string.Join("_", statuses.OrderBy(s => s))
                : "none";

            var result = await _courseRepository.GetAllCoursesAsync(filter, route, searchTerm, statuses);
            return result;
        }

        public async Task<CourseDTO?> GetCourseByIdAsync(int id)
        {
            var courseDto = await _courseRepository.GetCourseByIdAsync(id);
            return courseDto;
        }

        public async Task<CourseDTO> AddCourseAsync(int userId, CourseDTO courseDto)
        {
            try
            {
                var result = await _courseRepository.AddCourseAsync(userId, courseDto);
                InvalidateRelatedCaches();

                string cacheKey = $"Course_{result.Id}";
                SetCache(cacheKey, result);

                return result;
            }
            catch (KeyNotFoundException ex)
            {
                throw new Exception($"Tutor not found: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while adding the course.", ex);
            }
        }

        public async Task<CourseDTO> UpdateCourseAsync(int id, Course updatedCourse)
        {
            var existingCourse = await _courseRepository.GetCourseById(id);
            if (existingCourse == null)
                throw new Exception("Course does not exist!");

            existingCourse.CourseName = updatedCourse.CourseName;
            existingCourse.Description = updatedCourse.Description;
            existingCourse.StartDate = updatedCourse.StartDate;
            existingCourse.Subject = updatedCourse.Subject;
            existingCourse.Fee = updatedCourse.Fee;
            existingCourse.EndDate = updatedCourse.EndDate;
            existingCourse.MaxStudents = updatedCourse.MaxStudents;
            existingCourse.Status = updatedCourse.Status;

            await _courseRepository.UpdateCourseAsync(existingCourse);
            InvalidateCourseCache(id);
            InvalidateRelatedCaches();

            return new CourseDTO
            {
                Id = existingCourse.Id,
                CourseName = existingCourse.CourseName,
                Description = existingCourse.Description,
                TutorId = existingCourse.TutorId,
                Subject = existingCourse.Subject,
                MaxStudents = existingCourse.MaxStudents,
                Fee = existingCourse.Fee,
                StartDate = existingCourse.StartDate,
                EndDate = existingCourse.EndDate,
                Status = existingCourse.Status
            };
        }

        public async Task CancelCourseAsync(int id)
        {
            var course = await _courseRepository.GetCourseById(id);
            if (course == null)
                throw new Exception("Khóa học không tồn tại!");

            if (course.Status == "canceled" || course.Status == "completed")
                throw new Exception("Không thể hủy khóa học đã hủy hay đã hoàn thành!");

            if (course.Status == "ongoing" || course.Status == "coming")
            {
                var enrolledStudents = await _courseRepository.GetEnrolledStudentsByCourseIdAsync(id);
                foreach (var student in enrolledStudents)
                {
                    await _notificationRepo.CreateNotification(
                        student.UserId,
                        $"Khóa học '{course.CourseName}' đã bị hủy.",
                        "schedule_reminder"
                    );
                }
                InvalidateCourseCache(id);
            }

            course.Status = "canceled";
            await _courseRepository.UpdateCourseAsync(course);

            InvalidateCourseCache(id);
        }

        public async Task<bool> DeleteCoursesAsync(List<int> courseIds)
        {
            try
            {
                await _courseRepository.DeleteCoursesAsync(courseIds);

                foreach (var id in courseIds)
                {
                    InvalidateCourseCache(id);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SetCache<T>(string key, T value)
        {
            _cache.Set(key, value, TimeSpan.FromMinutes(CacheExpirationMinutes));
            _cacheKeys.TryAdd(key, true);
        }

        private void InvalidateCourseCache(int courseId)
        {
            string courseKey = $"Course_{courseId}";
            string studentsKey = $"StudentsByCourse_{courseId}";

            _cache.Remove(courseKey);
            _cache.Remove(studentsKey);
            _cacheKeys.TryRemove(courseKey, out _);
            _cacheKeys.TryRemove(studentsKey, out _);

            Console.WriteLine($"Cache removed: {courseKey}, {studentsKey}");

            var relatedCacheKeys = _cacheKeys.Keys
                .Where(k => k.StartsWith("CoursesByTutor_") ||
                            k.StartsWith("CoursesByStudent_") ||
                            k.StartsWith("GetAllCourses_"))
                .ToList();

            foreach (var key in relatedCacheKeys)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
                Console.WriteLine($"Cache removed: {key}");
            }
        }


        private void InvalidateRelatedCaches()
        {
            var allCacheKeys = _cacheKeys.Keys.ToList();

            foreach (var key in allCacheKeys)
            {
                if (key.StartsWith("CoursesByTutor_") ||
                    key.StartsWith("GetAllCourses_") ||
                    key.StartsWith("StudentsByCourse_") ||
                    key.StartsWith("Course_"))
                {
                    _cache.Remove(key);
                    _cacheKeys.TryRemove(key, out _);
                }
            }
        }
    }
}
