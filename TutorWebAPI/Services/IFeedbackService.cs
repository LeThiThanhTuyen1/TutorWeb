
using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface IFeedbackService
    {
        Task<bool> AddFeedbackAsync(Feedback feedback);
        Task<bool> UpdateFeedbackAsync(int feedbackId, Feedback feedback);
        Task<bool> DeleteFeedbackAsync(int feedbackId, int studentId);
        Task<List<FeedbackDTO>> GetTutorFeedbacksAsync(int tutorId);
        Task<FeedbackDTO> GetFeedbackByUserAsync(int tutorId, int studentId);
    }
}