using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Repositories;

namespace TutorWebAPI.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly ITutorService _tutorService; 
        private readonly ILogger<FeedbackService> _logger;
        private readonly IMemoryCache _cache;

        public FeedbackService(
            IFeedbackRepository feedbackRepository,
            ITutorService tutorService,
            ILogger<FeedbackService> logger,
            IMemoryCache cache)
        {
            _feedbackRepository = feedbackRepository;
            _tutorService = tutorService;
            _logger = logger;
            _cache = cache;
        }

        // Cache Key Helpers
        private string GetTutorFeedbackCacheKey(int tutorId) => $"Feedbacks_Tutor_{tutorId}";
        private string GetFeedbackCacheKey(int tutorId, int studentId) => $"Feedback_Tutor_{tutorId}_Student_{studentId}";

        // Add Feedback
        public async Task<bool> AddFeedbackAsync(Feedback feedback)
        {
            var result = await _feedbackRepository.AddFeedbackAsync(feedback);
            if (result)
            {
                _logger.LogInformation($"Feedback added for TutorId: {feedback.TutorId} by StudentId: {feedback.StudentId}");
                await InvalidateAndRefreshFeedbackCache(feedback.TutorId, feedback.StudentId);
                await RefreshTutorCache(feedback.TutorId); 
            }
            return result;
        }

        // Update Feedback
        public async Task<bool> UpdateFeedbackAsync(int feedbackId, Feedback feedback)
        {
            try
            {
                var result = await _feedbackRepository.UpdateFeedbackAsync(feedbackId, feedback);
                if (result)
                {
                    _logger.LogInformation($"Feedback {feedbackId} updated by StudentId: {feedback.StudentId}");
                    await InvalidateAndRefreshFeedbackCache(feedback.TutorId, feedback.StudentId);
                    await RefreshTutorCache(feedback.TutorId); 
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating feedback {feedbackId}");
                throw;
            }
        }

        // Delete Feedback
        public async Task<bool> DeleteFeedbackAsync(int feedbackId, int studentId)
        {
            try
            {
                var feedback = await _feedbackRepository.GetTutorFeedbacksAsync(0) 
                    .ContinueWith(t => t.Result.FirstOrDefault(f => f.Id == feedbackId));
                var tutorId = feedback?.TutorId ?? 0; 

                var result = await _feedbackRepository.DeleteFeedbackAsync(feedbackId, studentId);
                if (result)
                {
                    _logger.LogInformation($"Feedback {feedbackId} deleted by StudentId: {studentId}");
                    if (tutorId > 0) 
                    {
                        await InvalidateAndRefreshFeedbackCache(tutorId, studentId);
                        await RefreshTutorCache(tutorId); 
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting feedback {feedbackId}");
                throw;
            }
        }

        // Get Feedbacks by Tutor (with cache)
        public async Task<List<FeedbackDTO>> GetTutorFeedbacksAsync(int tutorId)
        {
            var feedbacks = await _feedbackRepository.GetTutorFeedbacksAsync(tutorId);
            await StoreTutorFeedbackInCache(tutorId, feedbacks);
            return feedbacks;
        }

        // Get Single Feedback (with cache disabled for now)
        public async Task<FeedbackDTO> GetFeedbackByUserAsync(int tutorId, int studentId)
        {
            var feedback = await _feedbackRepository.GetFeedbackByUserIdAsync(tutorId, studentId);
            return feedback;
        }

        // Helper to invalidate and refresh feedback cache
        private async Task InvalidateAndRefreshFeedbackCache(int tutorId, int studentId)
        {
            _cache.Remove(GetTutorFeedbackCacheKey(tutorId));
            _cache.Remove(GetFeedbackCacheKey(tutorId, studentId));
            await RefreshTutorFeedbackCache(tutorId);
        }

        // Helper to refresh tutor feedback cache
        private async Task RefreshTutorFeedbackCache(int tutorId)
        {
            var feedbacks = await _feedbackRepository.GetTutorFeedbacksAsync(tutorId);
            await StoreTutorFeedbackInCache(tutorId, feedbacks);
        }

        // Helper to refresh tutor cache
        private async Task RefreshTutorCache(int tutorId)
        {
            string tutorCacheKey = $"TutorById_{tutorId}";
            _cache.Remove(tutorCacheKey); 
            var tutor = await _tutorService.GetTutorByIdAsync(tutorId); 
            if (tutor != null)
            {
                _cache.Set(tutorCacheKey, tutor, GetCacheOptions());
                _logger.LogInformation($"Refreshed tutor cache for TutorId: {tutorId}");
            }
        }

        // Store feedbacks in cache
        private async Task StoreTutorFeedbackInCache(int tutorId, List<FeedbackDTO> feedbacks)
        {
            if (feedbacks != null && feedbacks.Count > 0)
            {
                _cache.Set(GetTutorFeedbackCacheKey(tutorId), feedbacks, GetCacheOptions());
                _logger.LogInformation($"Stored feedbacks for TutorId: {tutorId} in cache");
            }
        }

        // Common Cache Options
        private MemoryCacheEntryOptions GetCacheOptions()
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };
        }
    }
}