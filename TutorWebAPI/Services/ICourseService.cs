using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface ICourseService
    {
        Task<PagedResponse<List<CourseDTO>>> GetTutorCoursesByUserId(PaginationFilter filter, string route, int userId);
        Task<PagedResponse<List<EnrollmentDTO>>> GetStudentCoursesByUserId(PaginationFilter filter, string route, int userId);
        Task<List<StudentDTO>> GetStudentsByCourseIdAsync(int id);
        Task<PagedResponse<List<CourseDTO>>> GetAllCoursesAsync(
             PaginationFilter filter,
             string route,
             string? searchTerm = null,
             IEnumerable<string>? statuses = null);
        Task<CourseDTO?> GetCourseByIdAsync(int id);
        Task<CourseDTO> AddCourseAsync(int userId, CourseDTO courseDto);
        Task<CourseDTO> UpdateCourseAsync(int id, Course updatedCourse);
        Task CancelCourseAsync(int id);
        Task<bool> DeleteCoursesAsync(List<int> courseIds);
    }
}