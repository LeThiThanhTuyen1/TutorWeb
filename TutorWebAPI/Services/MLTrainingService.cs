using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TutorWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.Data;
using TutorWebAPI.Models.DTOs;

namespace TutorWebAPI.Services
{
    public class MLTrainingService : IMLTrainingService
    {
        private readonly MLContext _mlContext;
        private readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "tutor_model.zip");
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public MLTrainingService(ApplicationDbContext context, IMemoryCache cache)
        {
            _mlContext = new MLContext(seed: 0);
            _context = context;
            _cache = cache;
        }

        public void TrainModel()
        {
            var trainingData = LoadRealTrainingData();

            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(CompatibilityInput.ClassLevel),
                    nameof(CompatibilityInput.LocationMatch),
                    nameof(CompatibilityInput.SubjectMatch),
                    nameof(CompatibilityInput.Experience),
                    nameof(CompatibilityInput.Rating))
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Score", featureColumnName: "Features"));

            var model = pipeline.Fit(trainingData);

            using var stream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            _mlContext.Model.Save(model, trainingData.Schema, stream);
        }

        private IDataView LoadRealTrainingData()
        {
            var studentTutorPairs = _context.Enrollments
                .Include(e => e.Student)
                .ThenInclude(s => s.User)
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .ThenInclude(t => t.User)
                .GroupJoin(_context.Feedbacks,
                    e => new { e.StudentId, TutorId = e.Course.TutorId },
                    f => new { f.StudentId, f.TutorId },
                    (e, feedbacks) => new { Enrollment = e, Feedbacks = feedbacks })
                .SelectMany(x => x.Feedbacks.DefaultIfEmpty(), (x, f) => new
                {
                    StudentId = x.Enrollment.StudentId,
                    StudentClass = x.Enrollment.Student.Class ?? 0,
                    StudentLocation = x.Enrollment.Student.User.Location ?? string.Empty,
                    TutorExperience = x.Enrollment.Course.Tutor.Experience ?? 0,
                    TutorRating = x.Enrollment.Course.Tutor.Rating,
                    CourseSubject = x.Enrollment.Course.Subject ?? string.Empty,
                    EnrollmentStatus = x.Enrollment.Status,
                    FeedbackRating = f != null ? f.Rating : 0,
                    TutorLocation = x.Enrollment.Course.Tutor.User.Location ?? string.Empty
                })
                .ToList();

            var labeledData = studentTutorPairs.Select(p => new TrainingData
            {
                ClassLevel = p.StudentClass switch
                {
                    10 => 0.7f,
                    11 => 0.8f,
                    12 => 0.9f,
                    _ => 0.5f
                },
                LocationMatch = (p.StudentLocation == p.TutorLocation && !string.IsNullOrEmpty(p.StudentLocation)) ? 1f : 0f,
                SubjectMatch = CalculateSubjectMatch(GetSubjectPreference(p.StudentId), p.CourseSubject),
                Experience = Math.Min(p.TutorExperience / 10f, 1f),
                Rating = p.TutorRating / 5f,
                Score = CalculateCompatibilityScore(p.EnrollmentStatus, p.FeedbackRating)
            }).ToList();

            return _mlContext.Data.LoadFromEnumerable(labeledData);
        }

        private float CalculateSubjectMatch(List<string> studentSubjects, string courseSubject)
        {
            if (string.IsNullOrEmpty(courseSubject) || !studentSubjects.Any())
            {
                return 0f;
            }

            return studentSubjects.Any(subject => courseSubject.Contains(subject)) ? 1f : 0f;
        }

        private List<string> GetSubjectPreference(int studentId)
        {
            string cacheKey = $"SubjectPreference_{studentId}";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedSubjects))
            {
                return cachedSubjects;
            }

            var subjects = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId)
                .Select(e => e.Course.Subject)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            _cache.Set(cacheKey, subjects, cacheEntryOptions);

            return subjects.Any() ? subjects : new List<string> { "Unknown" };
        }

        private float CalculateCompatibilityScore(string enrollmentStatus, int feedbackRating)
        {
            float statusWeight = 0.6f;
            float feedbackWeight = 0.4f;

            float statusScore = enrollmentStatus.ToLower() == "completed" ? 90f :
                              enrollmentStatus.ToLower() == "pending" ? 70f : 50f;
            float feedbackScore = feedbackRating > 4 ? 90f :
                                feedbackRating > 3 ? 80f :
                                feedbackRating > 2 ? 70f : 50f;

            return (statusWeight * statusScore) + (feedbackWeight * feedbackScore);
        }

        private string GetSubjectFromEnrollment(Enrollment enrollment)
        {
            return enrollment.Course?.Subject ?? "Toán";
        }

        public ITransformer LoadModel()
        {
            if (!File.Exists(_modelPath))
            {
                TrainModel();
            }

            using var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return _mlContext.Model.Load(stream, out _);
        }
    }

    public class TrainingData
    {
        [ColumnName("ClassLevel")]
        public float ClassLevel { get; set; }

        [ColumnName("LocationMatch")]
        public float LocationMatch { get; set; }

        [ColumnName("SubjectMatch")]
        public float SubjectMatch { get; set; }

        [ColumnName("Experience")]
        public float Experience { get; set; }

        [ColumnName("Rating")]
        public float Rating { get; set; }

        [ColumnName("Score")]
        [LoadColumn(1)]
        public float Score { get; set; }
    }
}