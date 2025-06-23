using System.Threading.Tasks;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public interface ICourseRepository
    {
        Task<bool> CourseExistsAsync(int courseId);
        Task<PagedResponse<List<CourseDTO>>> GetAllCoursesAsync(PaginationFilter filter, string route, string? searchTerm = null, IEnumerable<string>? statuses = null);        
        Task<PagedResponse<List<CourseDTO>>> GetTutorCoursesByUserId(PaginationFilter filter, string route, int userId);
        Task<PagedResponse<List<EnrollmentDTO>>> GetStudentCoursesByUserId(PaginationFilter filter, string route, int userId);
        Task<CourseDTO?> GetCourseByIdAsync(int id);
        Task<CourseDTO> AddCourseAsync(int userId, CourseDTO courseDto);
        Task<CourseDTO> UpdateCourseAsync(Course Course);
        Task DeleteCoursesAsync(List<int> CoursesId);
        Task<List<StudentDTO>> GetEnrolledStudentsByCourseIdAsync(int courseId);
        Task<Course> GetCourseById(int id);
        Task<int> GetCourseCountAsync();
        //Task<string> GetCourseStatusAsync(int id);}
    }
}
