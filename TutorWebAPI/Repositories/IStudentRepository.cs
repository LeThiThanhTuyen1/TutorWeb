using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public interface IStudentRepository
    {
        Task<List<StudentDTO>> GetAllStudentsAsync();
        Task<bool> DeleteStudentsAsync(List<int> studentIds);
        Task<Student> GetStudentByUserIdAsync(int userId);
        Task<StudentStatsDTO> GetStudentStatsAsync(int userId);
        Task<List<SubjectPieDTO>> GetSubjectDistributionAsync(int userId);
        Task<List<StudentCourseDTO>> GetStudentCoursesAsync(int userId);
        Task<List<string>> GetSubjectsByUserIdAsync(int userId);
        Task<List<TutorDTO>> GetStudentTutorsAsync(int userId);
        Task<int> GetStudentCountAsync();
    }
}
