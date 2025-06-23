// Services/StudentService.cs
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Repositories;

namespace TutorWebAPI.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StudentService> _logger;
        private readonly IScoringService _scoringService;

        public StudentService(
            IStudentRepository studentRepository,
            IFeedbackRepository feedbackRepository,
            ITutorRepository tutorRepository,
            ICourseRepository courseRepository,
            IMemoryCache cache,
            ILogger<StudentService> logger,
            IScoringService scoringService)
        {
            _studentRepository = studentRepository;
            _cache = cache;
            _feedbackRepository = feedbackRepository;
            _courseRepository = courseRepository;
            _tutorRepository = tutorRepository;
            _logger = logger;
            _scoringService = scoringService;
        }

        public async Task<StudentStatsDTO> GetStudentStatsAsync(int userId)
        {
            var stats = await _studentRepository.GetStudentStatsAsync(userId);
            _logger.LogInformation("Cached student stats for student ID {StudentId}", userId);
            return stats;
        }

        public async Task<List<SubjectPieDTO>> GetSubjectDistributionAsync(int userId)
        {
            var subjects = await _studentRepository.GetSubjectDistributionAsync(userId);
            _logger.LogInformation("Cached subject distribution for student ID {StudentId}", userId);
            return subjects;
        }

        public async Task<List<StudentCourseDTO>> GetStudentCoursesAsync(int studentId)
        {
            var courses = await _studentRepository.GetStudentCoursesAsync(studentId);
            _logger.LogInformation("Cached courses for student ID {StudentId}", studentId);
            return courses;
        }

        public async Task<List<TutorDTO>> GetStudentTutorsAsync(int userId)
        {
            var tutors = await _studentRepository.GetStudentTutorsAsync(userId);
            _logger.LogInformation("Cached tutors for student ID {StudentId}", userId);
            return tutors;
        }

        public async Task<bool> DeleteStudentsAsync(List<int> studentIds)
        {
            return await _studentRepository.DeleteStudentsAsync(studentIds);
        }

        public async Task<List<object>> GetTutorRecommendationsAsync(int userId)
        {
            var student = await _studentRepository.GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new KeyNotFoundException("Học viên không tồn tại.");
            }

            var enrolledSubjects = await _studentRepository.GetSubjectsByUserIdAsync(userId);
            if (!enrolledSubjects.Any())
            {
                enrolledSubjects = new List<string> { "Unknown" };
            }

            var allTutors = new List<Tutor>();
            foreach (var enrolledSubject in enrolledSubjects)
            {
                var tutorss = await _tutorRepository.GetTutorsBySubjectAsync(enrolledSubject);
                allTutors.AddRange(tutorss);
            }
            var tutors = allTutors.DistinctBy(t => t.Id).ToList();
            if (!tutors.Any())
            {
                throw new KeyNotFoundException("Không tìm thấy gia sư đáp ứng môn học.");
            }
            
            var recommendations = new List<object>();
            foreach (var tutor in tutors)
            {
                var input = new CompatibilityInput
                {
                    ClassLevel = student.Class switch
                    {
                        10 => 0.7f,
                        11 => 0.8f,
                        12 => 0.9f,
                        _ => 0.5f
                    },
                    LocationMatch = (student.User.Location == tutor.User.Location && !string.IsNullOrEmpty(student.User.Location)) ? 1f : 0f,
                    SubjectMatch = tutor.Courses.Any(c => enrolledSubjects.Any(s => c.Subject.Contains(s))) ? 1f : 0f,
                    Experience = Math.Min(tutor.Experience ?? 0 / 10f, 1f),
                    Rating = tutor.Rating / 5f
                };

                var score = _scoringService.PredictCompatibility(input);

                recommendations.Add(new
                {
                    TutorId = tutor.Id,
                    TutorName = tutor.User.Name,
                    Subjects = tutor.Subjects,
                    Image = tutor.User.ProfileImage,
                    Experience = tutor.Experience,
                    Rating = tutor.Rating,
                    CompatibilityScore = score
                });
            }

            return recommendations.OrderByDescending(r => ((dynamic)r).CompatibilityScore).ToList();
        }

        public async Task<List<StatDTO>> GetStatsAsync()
        {
            const string cacheKey = "PlatformStats";

            if (_cache.TryGetValue(cacheKey, out List<StatDTO> cachedStats))
            {
                _logger.LogInformation("Retrieved stats from cache");
                return cachedStats;
            }

            _logger.LogInformation("Fetching stats from database");
            var studentCount = await _studentRepository.GetStudentCountAsync();
            var tutorCount = await _tutorRepository.GetTutorCountAsync();
            var courseCount = await _courseRepository.GetCourseCountAsync();
            var satisfactionRate = await _feedbackRepository.GetAverageSatisfactionRateAsync();

            var stats = new List<StatDTO>
            {
                new StatDTO { Id = 1, Value = $"{studentCount:N0}+", Label = "Học sinh" },
                new StatDTO { Id = 2, Value = $"{tutorCount:N0}+", Label = "Gia sư" },
                new StatDTO { Id = 3, Value = $"{courseCount:N0}+", Label = "Khóa học" },
                new StatDTO { Id = 4, Value = $"{satisfactionRate:F0}%", Label = "Đánh giá" }
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };

            _cache.Set(cacheKey, stats, cacheOptions);
            _logger.LogInformation("Cached stats");

            return stats;
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> source, Func<T, object> keySelector)
        {
            return source.GroupBy(keySelector).Select(g => g.First());
        }
    }
}
public class StatDTO
{
    public int Id { get; set; }
    public string Value { get; set; }
    public string Label { get; set; }
}