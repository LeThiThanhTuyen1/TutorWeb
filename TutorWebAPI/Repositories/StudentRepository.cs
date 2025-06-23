using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Services;

namespace TutorWebAPI.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ApplicationDbContext _context; 
        private readonly ITutorRepository _tutorRepository;
        private readonly IScoringService _scoringService;
        private readonly ILogger<StudentRepository> _logger;

        public StudentRepository(ApplicationDbContext context, ILogger<StudentRepository> logger, ITutorRepository tutorRepository,
            IScoringService scoringService)
        {
            _context = context; 
            _tutorRepository = tutorRepository;
            _scoringService = scoringService;
            _logger = logger;
        }

        public async Task<List<StudentDTO>> GetAllStudentsAsync()
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.User)
                    .ToListAsync();

                return students.Select(s => new StudentDTO
                {
                    Id = s.Id,
                    FullName = s.User!.Name,
                    Email = s.User.Email,
                    Phone = s.User.Phone,
                    School = s.User.School,
                    Location = s.User.Location,
                    ProfileImage = s.User.ProfileImage
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all students.");
                throw new Exception("An error occurred while retrieving students.");
            }
        }

        public async Task<bool> DeleteStudentsAsync(List<int> studentIds)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.Id))
                    .ToListAsync();

                if (!students.Any())
                {
                    _logger.LogWarning("No students found for the provided IDs: {StudentIds}", studentIds);
                    return false;
                }

                _context.Students.RemoveRange(students);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted students with IDs: {StudentIds}", studentIds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting multiple students with IDs: {StudentIds}", studentIds);
                throw new Exception("An error occurred while deleting students.");
            }
        }
        public async Task<StudentStatsDTO> GetStudentStatsAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddTicks(-1);

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Schedules)
                .Where(e => e.Student.UserId == userId)
                .ToListAsync();

            double CalculateTotalHours(List<Enrollment> enrollments)
            {
                double hours = 0;
                foreach (var e in enrollments)
                {
                    var course = e.Course;
                    if (course.Schedules == null) continue;

                    foreach (var schedule in course.Schedules)
                    {
                        if (schedule.StartHour != null && schedule.EndHour != null)
                        {
                            double hoursPerSession = (schedule.EndHour - schedule.StartHour).TotalHours;
                            int count = CountMatchingWeekdays(course.StartDate, course.EndDate, schedule.DayOfWeek.ToString());
                            hours += count * hoursPerSession;
                        }
                    }
                }
                return hours;
            }

            var currentEnrollments = enrollments
                .Where(e => e.EnrolledAt >= currentMonthStart)
                .ToList();

            var lastMonthEnrollments = enrollments
                .Where(e => e.EnrolledAt >= lastMonthStart && e.EnrolledAt <= lastMonthEnd)
                .ToList();

            var lastMonthCompleted = enrollments
                .Where(e => e.Course.Status == "completed" &&
                            e.Course.StartDate >= lastMonthStart &&
                            e.Course.EndDate <= lastMonthEnd)
                .ToList();

            return new StudentStatsDTO
            {
                Courses = enrollments.Count,
                CoursesLastMonth = lastMonthEnrollments.Count,
                CompletedCourses = enrollments.Count(e => e.Course.Status == "completed"),
                CompletedCoursesLastMonth = lastMonthCompleted.Count,
                HoursLearned = (int)CalculateTotalHours(enrollments),
                HoursLearnedLastMonth = (int)CalculateTotalHours(lastMonthEnrollments)
            };
        }

        private int CountMatchingWeekdays(DateTime start, DateTime end, string? dayOfWeek)
        {
            if (!Enum.TryParse<DayOfWeek>(dayOfWeek, true, out var targetDay))
                return 0;

            int count = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == targetDay)
                    count++;
            }
            return count;
        }

        public async Task<List<SubjectPieDTO>> GetSubjectDistributionAsync(int userId)
        {
            var courses = await _context.Enrollments
                .Where(e => e.Student.UserId == userId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .Select(e => e.Course)
                .ToListAsync();

            var subjectCounts = courses
                .SelectMany(c => (c.Tutor.Subjects ?? "Other").Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .GroupBy(s => s)
                .Select(g => new SubjectPieDTO
                {
                    Name = g.Key,
                    Value = g.Count(),
                    Color = GetColorForSubject(g.Key)
                })
                .Where(dto => !string.IsNullOrEmpty(dto.Name))
                .ToList();

            return subjectCounts;
        }

        public async Task<List<StudentCourseDTO>> GetStudentCoursesAsync(int userId)
        {
            return await _context.Enrollments
                .Where(e => e.Student.UserId == userId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .ThenInclude(t => t.User)
                .Include(e => e.Course.Schedules)
                .Select(e => new StudentCourseDTO
                {
                    Id = e.Course.Id,
                    Name = e.Course.CourseName,
                    Tutor = e.Course.Tutor.User.Name,
                    Progress = e.Course.Status == "completed"
                        ? 100
                        : e.Course.Status == "coming"
                            ? 0
                            : CalculateProgress(e.Course.StartDate),
                    NextLesson = e.Course.Schedules.Any() ? e.Course.Schedules.First().StartHour.ToString() : "N/A"
                })
                .ToListAsync();
        }

        private static int CalculateProgress(DateTime startDate)
        {
            var daysSinceStart = (int)(DateTime.Now - startDate).TotalDays;
            return daysSinceStart < 0 ? 0 : Math.Min(daysSinceStart % 100, 100);
        }

        public async Task<List<TutorDTO>> GetStudentTutorsAsync(int userId)
        {
            return await _context.Enrollments
                .Where(e => e.Student.UserId == userId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .ThenInclude(t => t.User)
                .Select(e => new TutorDTO
                {
                    Id = e.Course.Tutor.Id,
                    TutorName = e.Course.Tutor.User.Name,
                    Subjects = e.Course.Tutor.Subjects ?? "N/A",
                    ProfileImage = e.Course.Tutor.User.ProfileImage ?? "/placeholder.svg?height=40&width=40",
                    Rating = e.Course.Tutor.Rating
                })
                .Distinct()
                .ToListAsync();
        }

        public async Task<int> GetStudentCountAsync()
        {
            return await _context.Students.CountAsync();
        }

        public async Task<List<string>> GetSubjectsByUserIdAsync(int userId)
        {
            return await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Student.UserId == userId)
                .Select(e => e.Course.Subject)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToListAsync() ?? new List<string> { "Unknown" };
        }

        public async Task<Student> GetStudentByUserIdAsync(int userId)
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        private string GetColorForSubject(string subject)
        {
            return subject.ToLower() switch
            {
                "lý" => "#f97316",
                "toán" => "#82ca9d",
                "sinh" => "#8884d8",
                "anh" => "#ffc658",
                _ => "#d3d3d3"
            };
        }
    }
}
