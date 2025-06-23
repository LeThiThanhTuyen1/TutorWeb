using GoogleApi.Entities.Search.Video.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriRepository _uriRepository;
        private readonly ILogger<EnrollmentRepository> _logger;

        public EnrollmentRepository(ApplicationDbContext context, IUriRepository uriRepository, ILogger<EnrollmentRepository> logger)
        {
            _context = context;
            _uriRepository = uriRepository;
            _logger = logger;
        }

        public async Task<bool> IsStudentEligibleAsync(int studentId, int courseId)
        {
            try
            {
                return !await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking eligibility for student {StudentId} and course {CourseId}", studentId, courseId);
                throw new Exception("An error occurred while checking the student's eligibility. Please try again later.");
            }
        }

        public async Task<int> GetEnrolledStudentCountAsync(int courseId)
        {
            try
            {
                return await _context.Enrollments
                    .Where(e => e.CourseId == courseId && e.Status != "canceled")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while counting enrolled students for course {CourseId}", courseId);
                throw new Exception("An error occurred while counting the enrolled students. Please try again later.");
            }
        }

        public async Task<int> GetMaxStudentsAsync(int courseId)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
                return course?.MaxStudents ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching max students for course {CourseId}", courseId);
                throw new Exception("An error occurred while fetching the max students for the course. Please try again later.");
            }
        }

        public async Task EnrollStudentAsync(Enrollment enrollment)
        {
            try
            {
                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while enrolling student {StudentId} to course {CourseId}", enrollment.StudentId, enrollment.CourseId);
                throw new Exception("An error occurred while enrolling the student. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while enrolling student.");
                throw new Exception("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<Student?> GetStudentByIdAsync(int studentId)
        {
            try
            {
                return await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the student with id {StudentId}", studentId);
                throw new Exception("An error occurred while fetching the student's information. Please try again later.");
            }
        }

        public async Task<List<Course>> GetStudentEnrolledCoursesAsync(int userId)
        {
            try
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for userId {UserId}", userId);
                    return new List<Course>();
                }

                int studentId = student.Id;

                return await _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.Course)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching enrolled courses for userId {UserId}", userId);
                throw new Exception("An error occurred while fetching the enrolled courses. Please try again later.");
            }
        }

        public async Task<bool> UnenrollStudentAsync(int userId, int courseId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Student)
                    .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(e => e.Student.UserId == userId && e.CourseId == courseId);

                if (enrollment == null)
                {
                    _logger.LogWarning("Enrollment not found for student {StudentId} and course {CourseId}", enrollment.StudentId, courseId);
                    return false;
                }

                if (enrollment.Course.Status == "canceled")
                {
                    _logger.LogWarning("Cannot unenroll from course {CourseId} because status is {Status}, not 'coming'", courseId, enrollment.Course.Status);
                    return false;
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} successfully unenrolled from course {CourseId}", enrollment.StudentId, courseId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while unenrolling student with userId {userId} from course {CourseId}", userId, courseId);
                throw new Exception("An error occurred while unenrolling the student. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while unenrolling with userId student {userId} from course {CourseId}", userId, courseId);
                throw new Exception("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<EnrollmentDTO> GetEnrollmentByIdAsync(int enrollmentId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Student)
                    .Include(e => e.Course.Tutor) 
                    .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(u => u.Id == enrollmentId);

                if (enrollment == null)
                {
                    throw new KeyNotFoundException($"Enrollment with ID {enrollmentId} not found.");
                }

                return new EnrollmentDTO
                {
                    Id = enrollment.Id,
                    UserId = enrollment.Student.UserId,
                    CourseId = enrollment.CourseId,
                    TutorName = enrollment.Course.Tutor.User.Name, 
                    CourseName = enrollment.Course.CourseName,
                    StartDate = enrollment.Course.StartDate,
                    EndDate = enrollment.Course.EndDate,
                    Fee = enrollment.Course.Fee,
                    Status = enrollment.Status,
                    EnrolledAt = enrollment.EnrolledAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enrollment with ID {EnrollmentId}", enrollmentId);
                throw new Exception($"An error occurred while retrieving enrollment with ID {enrollmentId}: {ex.Message}", ex);
            }
        }
    }
}
