using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface IEnrollmentService
    {
        Task<Response<string>> RegisterStudentForCourse(int courseId, int userId);
        Task<bool> UnenrollStudentAsync(int userId, int courseId);
        Task<EnrollmentDTO> GetEnrollmentById(int enrollmentId);
        Task SendNotification(int userId, string message);
        Task<List<Course>> GetStudentCourses(int studentId);
    }
}