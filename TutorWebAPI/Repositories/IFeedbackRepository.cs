using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public interface IFeedbackRepository
    {
        Task<bool> AddFeedbackAsync(Feedback feedback);
        Task<FeedbackDTO> GetFeedbackByUserIdAsync(int tutorId, int userId);
        Task<bool> UpdateFeedbackAsync(int feedbackId, Feedback feedback);
        Task<bool> DeleteFeedbackAsync(int feedbackId, int studentId);
        Task UpdateTutorRatingAsync(int tutorId);
        Task<List<FeedbackDTO>> GetTutorFeedbacksAsync(int tutorId);
        Task<float> CalculateTutorRatingAsync(int tutorId);
        Task<float> GetAverageSatisfactionRateAsync();
    }
}
