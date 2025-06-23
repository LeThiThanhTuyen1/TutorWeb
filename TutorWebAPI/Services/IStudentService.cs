
using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface IStudentService
    {
        Task<StudentStatsDTO> GetStudentStatsAsync(int userId);
        Task<List<SubjectPieDTO>> GetSubjectDistributionAsync(int userId);
        Task<List<StudentCourseDTO>> GetStudentCoursesAsync(int studentId);
        Task<List<TutorDTO>> GetStudentTutorsAsync(int userId);
        Task<bool> DeleteStudentsAsync(List<int> studentIds);
        Task<List<StatDTO>> GetStatsAsync();
        //Task<List<object>> GetTutorRecommendationsAsync(int userId, string subject = null);
        Task<List<object>> GetTutorRecommendationsAsync(int userId);
    }
}