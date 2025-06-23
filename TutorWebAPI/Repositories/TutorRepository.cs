using System.Globalization;
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
    public class TutorRepository : ITutorRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriRepository _uriRepository;
        private readonly ILogger<TutorRepository> _logger;

        public TutorRepository(ApplicationDbContext context, IUriRepository uriRepository, ILogger<TutorRepository> logger)
        {
            _context = context;
            _uriRepository = uriRepository;
            _logger = logger;
        }

        public async Task<List<Tutor>> GetTutorsBySubjectAsync(string subject)
        {
            return await _context.Tutors
                .Include(t => t.User)
                .Include(t => t.Courses)
                .Where(t => t.Courses.Any(c => c.Subject.Contains(subject) && !c.isDeleted))
                .ToListAsync();
        }

        public async Task<PagedResponse<List<TutorDTO>>> GetAllTutors(PaginationFilter filter, string route)
        {
            try
            {
                var tutorsQuery = _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.Courses)
                        .ThenInclude(c => c.Schedules)
                    .AsQueryable();

                var tutorDTOQuery = tutorsQuery.Select(t => new TutorDTO
                {
                    Id = t.Id,
                    TutorName = t.User!.Name,
                    Subjects = t.Subjects,
                    Introduction = t.Introduction,
                    Gender = t.User!.Gender,
                    School = t.User!.School,
                    Email = t.User!.Email,
                    Rating = t.Rating,
                    Experience = t.Experience.GetValueOrDefault(),
                    Location = t.User!.Location,
                    ProfileImage = t.User!.ProfileImage,
                    FeeRange = new FeeRangeDTO
                    {
                        MinFee = t.Courses.Any() ? t.Courses.Min(c => c.Fee) : 0,
                        MaxFee = t.Courses.Any() ? t.Courses.Max(c => c.Fee) : 0
                    },
                    TeachingModes = t.Courses
                        .Where(c => c.Schedules != null)
                        .SelectMany(c => c.Schedules!.Where(s => s.Mode != null).Select(s => s.Mode!))
                        .Distinct()
                        .ToList()
                });

                return await tutorDTOQuery
                    .OrderByDescending(t => t.Rating)
                    .ToPagedResponseAsync(filter, _uriRepository, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching tutors.");
                throw new Exception("An error occurred while fetching tutors.");
            }
        }

        public async Task<PagedResponse<List<TutorDTO>>> SearchTutors(TutorSearchDTO searchCriteria, PaginationFilter filter, string route)
        {
            try
            {
                var tutorsQuery = _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.Courses)
                        .ThenInclude(c => c.Schedules)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchCriteria.Subjects))
                {
                    var subjectList = searchCriteria.Subjects
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();

                    tutorsQuery = tutorsQuery.Where(t => subjectList.Any(subject => t.Subjects.Contains(subject)));
                }

                if (!string.IsNullOrEmpty(searchCriteria.Location))
                    tutorsQuery = tutorsQuery.Where(t => t.User != null && t.User.Location.Contains(searchCriteria.Location));

                if (!string.IsNullOrEmpty(searchCriteria.TeachingMode))
                    tutorsQuery = tutorsQuery.Where(t => t.Courses.Any(c => c.Schedules.Any(s => s.Mode == searchCriteria.TeachingMode)));

                if (searchCriteria.MinFee.HasValue)
                    tutorsQuery = tutorsQuery.Where(t => t.Courses.Any(c => c.Fee >= searchCriteria.MinFee.Value));

                if (searchCriteria.MaxFee.HasValue)
                    tutorsQuery = tutorsQuery.Where(t => t.Courses.Any(c => c.Fee <= searchCriteria.MaxFee.Value));

                if (searchCriteria.MinRating.HasValue)
                    tutorsQuery = tutorsQuery.Where(t => t.Rating >= searchCriteria.MinRating.Value);

                if (searchCriteria.MinExperience.HasValue)
                    tutorsQuery = tutorsQuery.Where(t => t.Experience >= searchCriteria.MinExperience.Value);

                var tutorDTOQuery = tutorsQuery.Select(t => new TutorDTO
                {
                    Id = t.Id,
                    TutorName = t.User!.Name,
                    Subjects = t.Subjects,
                    Introduction = t.Introduction,
                    Gender = t.User!.Gender,
                    School = t.User!.School,
                    Email = t.User!.Email,
                    Rating = t.Rating,
                    Experience = t.Experience.GetValueOrDefault(),
                    Location = t.User!.Location,
                    ProfileImage = t.User!.ProfileImage,
                    FeeRange = new FeeRangeDTO
                    {
                        MinFee = t.Courses.Any() ? t.Courses.Min(c => c.Fee) : 0,
                        MaxFee = t.Courses.Any() ? t.Courses.Max(c => c.Fee) : 0
                    },
                    TeachingModes = t.Courses
                        .Where(c => c.Schedules != null)
                        .SelectMany(c => c.Schedules!.Where(s => s.Mode != null).Select(s => s.Mode!))
                        .Distinct()
                        .ToList()
                });

                return await tutorDTOQuery
                    .OrderByDescending(t => t.Rating)
                    .ToPagedResponseAsync(filter, _uriRepository, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for tutors with criteria {Criteria}.", searchCriteria);
                throw new Exception("An error occurred while searching for tutors.");
            }
        }

        public async Task<object> GetStatsAsync(int userId)
        {
            var tutor = await _context.Tutors
                .Where(t => t.UserId == userId)
                .Include(t => t.Courses)
                    .ThenInclude(c => c.Schedules)
                .Include(t => t.Courses)
                    .ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync();

            if (tutor == null)
            {
                return new { courses = 0, students = 0, hours = 0, rating = 0f };
            }

            int totalCourses = tutor.Courses.Count;
            int totalStudents = tutor.Courses.SelectMany(c => c.Enrollments ?? new List<Enrollment>()).Count();

            double totalHours = 0;

            foreach (var course in tutor.Courses)
            {
                if (course.Schedules == null) continue;

                foreach (var schedule in course.Schedules)
                {
                    if (schedule.StartHour != null && schedule.EndHour != null)
                    {
                        double hoursPerSession = (schedule.EndHour - schedule.StartHour).TotalHours;
                        int count = CountMatchingWeekdays(course.StartDate, course.EndDate, schedule.DayOfWeek.ToString());
                        totalHours += count * hoursPerSession;
                    }
                }
            }

            return new
            {
                courses = totalCourses,
                students = totalStudents,
                hours = (int)totalHours,
                rating = tutor.Rating
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

        public async Task<List<object>> GetBarDataAsync(int userId)
        {
            return await _context.Enrollments
                .Where(e => e.Course.Tutor.UserId == userId)
                .GroupBy(e => e.EnrolledAt.Month)
                .Select(g => (object)new
                {
                    name = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                    students = g.Count(),
                    hours = g.Sum(e => e.Course.Schedules.Sum(s => EF.Functions.DateDiffHour(s.StartHour, s.EndHour)))
                })
                .Take(6)
                .ToListAsync();
        }

        public async Task<List<object>> GetUpcomingClassesAsync(int userId)
        {
            try
            {
                var now = DateTime.Now;
                var rawSchedules = await _context.Schedules
                    .Include(s => s.Course)
                    .ThenInclude(c => c.Enrollments)
                    .Where(s => s.Tutor.UserId == userId)
                    .ToListAsync();

                var upcoming = rawSchedules
                    .Select(s => new
                    {
                        Schedule = s,
                        FullStartTime = s.Course.StartDate.Date + s.StartHour,
                        FullEndTime = s.Course.StartDate.Date + s.EndHour
                    })
                    .Where(x => x.FullStartTime >= now)
                    .OrderBy(x => x.FullStartTime)
                    .Take(3)
                    .Select(x => (object)new
                    {
                        id = x.Schedule.Id,
                        courseName = x.Schedule.Course?.CourseName ?? "Không có thông tin Course",
                        time = $"{x.FullStartTime:dddd, hh:mm tt} - {x.FullEndTime:hh:mm tt}",
                        students = x.Schedule.Course?.Enrollments?.Count() ?? 0,
                        mode = x.Schedule.Mode ?? "Không có thông tin"
                    })
                    .ToList();

                return upcoming;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUpcomingClassesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<object>> GetRecentContractsAsync(int userId)
        {
            return await _context.Contracts
                .Where(c => c.Tutor.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(4)
                .Select(c => (object)new
                {
                    id = c.Id,
                    student = c.Student.User.Name,
                    studentProfile = c.Student.User.ProfileImage,
                    contractDetails = $"Course: {c.Course.CourseName}",
                    time = $"{(DateTime.Now - c.CreatedAt).TotalHours:F1} hours ago",
                    status = c.Status
                })
                .ToListAsync();
        }

        public async Task<TutorDTO?> GetTutorByUserIdAsync(int userId)
        {
            try
            {
                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.Courses)
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                    return null;

                return new TutorDTO
                {
                    Id = tutor.Id,
                    TutorName = tutor.User!.Name,
                    Subjects = tutor.Subjects,
                    Introduction = tutor.Introduction,
                    Rating = tutor.Rating,
                    Gender = tutor.User!.Gender,
                    School = tutor.User!.School,
                    Email = tutor.User!.Email,
                    Experience = tutor.Experience.GetValueOrDefault(),
                    Location = tutor.User!.Location,
                    ProfileImage = tutor.User!.ProfileImage,
                    FeeRange = new FeeRangeDTO
                    {
                        MinFee = tutor.Courses.Any() ? tutor.Courses.Min(c => c.Fee) : 0,
                        MaxFee = tutor.Courses.Any() ? tutor.Courses.Max(c => c.Fee) : 0
                    },
                    TeachingModes = tutor.Courses
                        .Where(c => c.Schedules != null)
                        .SelectMany(c => c.Schedules!.Where(s => s.Mode != null).Select(s => s.Mode!))
                        .Distinct()
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving tutor by user ID {UserId}.", userId);
                throw new Exception("An error occurred while retrieving the tutor.");
            }
        }

        public async Task<TutorDTO?> GetTutorByIdAsync(int tutorId)
        {
            try
            {
                var tutor = await _context.Tutors
                    .Include(t => t.User)
                    .Include(t => t.Courses)
                    .FirstOrDefaultAsync(t => t.Id == tutorId);

                if (tutor == null)
                    return null;

                var teachingModes = await _context.Schedules
                    .Where(s => s.TutorId == tutorId && s.Mode != null)
                    .Select(s => s.Mode!)
                    .Distinct()
                    .ToListAsync();

                var courses = tutor.Courses.Select(c => new CourseDTO
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    Fee = c.Fee,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                }).ToList();

                return new TutorDTO
                {
                    Id = tutor.Id,
                    TutorName = tutor.User?.Name ?? "Không có thông tin",
                    Subjects = tutor.Subjects,
                    Introduction = tutor.Introduction,
                    Rating = tutor.Rating,
                    Gender = tutor.User?.Gender ?? "Không có thông tin",
                    School = tutor.User?.School ?? "Không có thông tin",
                    Email = tutor.User?.Email ?? "Không có thông tin",
                    Experience = tutor.Experience.GetValueOrDefault(),
                    Location = tutor.User?.Location ?? "Không có thông tin",
                    ProfileImage = tutor.User?.ProfileImage ?? string.Empty,
                    FeeRange = new FeeRangeDTO
                    {
                        MinFee = tutor.Courses.Any() ? tutor.Courses.Min(c => c.Fee) : 0,
                        MaxFee = tutor.Courses.Any() ? tutor.Courses.Max(c => c.Fee) : 0
                    },
                    TeachingModes = teachingModes,
                    Courses = courses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving tutor by ID {TutorId}.", tutorId);
                throw new Exception("An error occurred while retrieving the tutor.");
            }
        }

        public async Task<int> GetTutorCountAsync()
        {
            return await _context.Tutors.CountAsync();
        }

        public async Task<bool> TutorExistsAsync(int tutorId)
        {
            try
            {
                return await _context.Tutors.AnyAsync(t => t.Id == tutorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if tutor {TutorId} exists.", tutorId);
                throw new Exception("An error occurred while checking tutor existence.");
            }
        }

        public async Task<bool> DeleteTutorsAsync(List<int> tutorIds)
        {
            try
            {
                var tutors = await _context.Tutors
                    .Where(t => tutorIds.Contains(t.Id))
                    .ToListAsync();

                if (!tutors.Any())
                {
                    _logger.LogWarning("No tutors found for the provided IDs: {TutorIds}", tutorIds);
                    return false;
                }

                _context.Tutors.RemoveRange(tutors);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted tutors with IDs: {TutorIds}", tutorIds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting multiple tutors with IDs: {TutorIds}", tutorIds);
                throw new Exception("An error occurred while deleting tutors.");
            }
        }
    }
}
