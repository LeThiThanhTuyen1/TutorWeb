using System.Collections.Generic;
using System.Threading.Tasks;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public interface IEnrollmentRepository
    {
        Task<bool> IsStudentEligibleAsync(int studentId, int courseId);
        Task<int> GetEnrolledStudentCountAsync(int courseId);
        Task<int> GetMaxStudentsAsync(int courseId);
        Task EnrollStudentAsync(Enrollment enrollment);
        Task<List<Course>> GetStudentEnrolledCoursesAsync(int studentId);
        Task<Student?> GetStudentByIdAsync(int studentId);
        Task<EnrollmentDTO> GetEnrollmentByIdAsync(int enrollmentId);
        Task<bool> UnenrollStudentAsync(int userId, int courseId);
    }
}
