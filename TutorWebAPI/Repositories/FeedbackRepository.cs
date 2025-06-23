using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackRepository> _logger;

        public FeedbackRepository(ApplicationDbContext context, ILogger<FeedbackRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AddFeedbackAsync(Feedback feedback)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var tutor = await _context.Tutors
                    .FirstOrDefaultAsync(t => t.Id == feedback.TutorId);
                if (tutor == null)
                {
                    _logger.LogWarning("Tutor {TutorId} not found for feedback", feedback.TutorId);
                    return false;
                }

                var existingFeedback = await _context.Feedbacks
                    .AnyAsync(f => f.StudentId == feedback.StudentId && f.TutorId == feedback.TutorId);
                if (existingFeedback)
                {
                    _logger.LogWarning("Feedback already exists for student {StudentId} and tutor {TutorId}", feedback.StudentId, feedback.TutorId);
                    return false;
                }

                feedback.CreatedAt = DateTime.UtcNow;
                _context.Feedbacks.Add(feedback);

                await UpdateTutorRatingAsync(feedback.TutorId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Added feedback for tutor {TutorId} by student {StudentId}", feedback.TutorId, feedback.StudentId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding feedback for tutor {TutorId} and student {StudentId}", feedback.TutorId, feedback.StudentId);
                throw new Exception("An error occurred while adding feedback. Please try again later.", ex);
            }
        }

        public async Task<bool> UpdateFeedbackAsync(int feedbackId, Feedback feedback)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var feedbackEx = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.Id == feedbackId);
                if (feedbackEx == null)
                {
                    _logger.LogWarning("Feedback {FeedbackId} not found", feedbackId);
                    return false;
                }

                if (feedbackEx.StudentId != feedback.StudentId)
                {
                    _logger.LogWarning("Unauthorized attempt to update feedback {FeedbackId} by student {StudentId}", feedbackId, feedback.StudentId);
                    throw new UnauthorizedAccessException("You do not have permission to edit this review.");
                }

                feedbackEx.Rating = feedback.Rating;
                feedbackEx.Comment = feedback.Comment;

                await UpdateTutorRatingAsync(feedbackEx.TutorId);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Updated feedback {FeedbackId} for tutor {TutorId}", feedbackId, feedbackEx.TutorId);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Unauthorized attempt to update feedback {FeedbackId}", feedbackId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating feedback {FeedbackId}", feedbackId);
                throw new Exception("An error occurred while updating feedback. Please try again later.", ex);
            }
        }

        public async Task<bool> DeleteFeedbackAsync(int feedbackId, int studentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var feedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.Id == feedbackId);
                if (feedback == null)
                {
                    _logger.LogWarning("Feedback {FeedbackId} not found", feedbackId);
                    return false;
                }

                if (feedback.StudentId != studentId)
                {
                    _logger.LogWarning("Unauthorized attempt to delete feedback {FeedbackId} by student {StudentId}", feedbackId, studentId);
                    throw new UnauthorizedAccessException("You do not have permission to delete this review.");
                }

                _context.Feedbacks.Remove(feedback);
                await UpdateTutorRatingAsync(feedback.TutorId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted feedback {FeedbackId} for tutor {TutorId}", feedbackId, feedback.TutorId);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Unauthorized attempt to delete feedback {FeedbackId}", feedbackId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting feedback {FeedbackId}", feedbackId);
                throw new Exception("An error occurred while deleting feedback. Please try again later.", ex);
            }
        }

        public async Task<List<FeedbackDTO>> GetTutorFeedbacksAsync(int tutorId)
        {
            try
            {
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.TutorId == tutorId)
                    .Include(f => f.Student)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new FeedbackDTO
                    {
                        Id = f.Id,
                        StudentId = f.StudentId,
                        StudentName = f.Student.User.Name,
                        StudentImg = f.Student.User.ProfileImage,
                        TutorId = f.TutorId,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt,
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} feedbacks for tutor {TutorId}", feedbacks.Count, tutorId);
                return feedbacks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedbacks for tutor {TutorId}", tutorId);
                throw new Exception("An error occurred while retrieving feedbacks. Please try again later.", ex);
            }
        }

        public async Task<FeedbackDTO> GetFeedbackByUserIdAsync(int tutorId, int userId)
        {
            try
            {
                var studentId = await _context.Students
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                if (studentId == 0)
                {
                    _logger.LogWarning("Student not found for userId {UserId}", userId);
                    throw new Exception("Student not found for the provided userId.");
                }

                var feedback = await _context.Feedbacks
                    .Where(f => f.TutorId == tutorId && f.StudentId == studentId)
                    .Include(f => f.Student)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new FeedbackDTO
                    {
                        Id = f.Id,
                        StudentId = f.StudentId,
                        StudentName = f.Student.User.Name,
                        StudentImg = f.Student.User.ProfileImage,
                        TutorId = f.TutorId,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Retrieved feedback for tutor {TutorId} and student {StudentId}", tutorId, studentId);
                return feedback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for tutor {TutorId} and user {UserId}", tutorId, userId);
                throw new Exception("An error occurred while retrieving feedback. Please try again later.", ex);
            }
        }

        public async Task<float> GetAverageSatisfactionRateAsync()
        {
            try
            {
                var avgRating = await _context.Feedbacks
                    .AverageAsync(f => (float?)f.Rating) ?? 0f;
                var satisfactionRate = avgRating / 5f * 100f;
                _logger.LogInformation("Calculated average satisfaction rate: {Rate}%", satisfactionRate);
                return satisfactionRate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average satisfaction rate");
                throw new Exception("An error occurred while calculating satisfaction rate.", ex);
            }
        }

        public async Task<float> CalculateTutorRatingAsync(int tutorId)
        {
            try
            {
                var averageRating = await _context.Feedbacks
                    .Where(f => f.TutorId == tutorId)
                    .AverageAsync(f => (float?)f.Rating) ?? 0f;
                _logger.LogInformation("Calculated rating for tutor {TutorId}: {Rating}", tutorId, averageRating);
                return averageRating;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating rating for tutor {TutorId}", tutorId);
                throw new Exception("An error occurred while calculating the tutor's rating. Please try again later.", ex);
            }
        }

       public async Task UpdateTutorRatingAsync(int tutorId)
        {
            try
            {
                var tutor = await _context.Tutors.FindAsync(tutorId);
                if (tutor == null)
                {
                    _logger.LogWarning("Tutor {TutorId} not found for rating update", tutorId);
                    return;
                }

                var averageRating = await _context.Feedbacks
                    .Where(f => f.TutorId == tutorId)
                    .AverageAsync(f => (float?)f.Rating) ?? 0f;

                tutor.Rating = averageRating;
                _context.Entry(tutor).State = EntityState.Modified;

                _logger.LogInformation("Updated rating for tutor {TutorId} to {Rating}", tutorId, averageRating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating for tutor {TutorId}", tutorId);
                throw new Exception("An error occurred while updating tutor rating.", ex);
            }
        }
    }
}