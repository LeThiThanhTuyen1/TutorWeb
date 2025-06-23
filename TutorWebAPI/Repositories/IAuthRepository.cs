using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public interface IAuthRepository
    {
        Task<bool> IsEmailExist(string email);
        Task<Student> GetStudentByUserIdAsync(int userId);
        Task<User?> GetUserByEmail(string email);
        Task<ProfileDTO?> GetProfileById(int userId);
        Task<User?> GetUserById(int userId);
        Task<int> GetUserIdByTutorId(int tutorId);
        Task RegisterUser(User user);
        Task<bool> ValidateUser(string email, string password);
        Task<bool> UpdatePassword(int userId, string newPassword);
        Task<bool> UpdateProfile(int userId, ProfileDTO updatedProfile);
        Task<string> UploadProfileImage(int userId, IFormFile file);
        //Task<User> GetUserByRefreshToken(string refreshToken);
        Task<bool> UpdateUser(User user);
        Task RemoveRefreshToken(int userId);
        Task<bool> DeleteAccount(int userId);
    }
}
