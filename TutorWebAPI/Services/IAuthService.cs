using TutorWebAPI.Wrapper;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace TutorWebAPI.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> Logout();
        Task<Response<ProfileDTO>> GetProfile(int userId);
        Task<bool> IsEmailExist(string email);
        Task<User> GetUserByEmail(string email);
        Task<Response<bool>> RegisterUser(RegisterDTO dto);
        //Task<User> GetUserByRefreshToken(string refreshToken);
        Task<bool> UpdateUser(User user);
        Task<Response<bool>> VerifyUser(string email, string code);
        Task<bool> ResendVerificationCode(string email);
        Task<bool> ForgotPassword(string email);
        Task<Response<bool>> ResetPassword(ResetPasswordRequest request);
        Task<Response<LoginResponse?>> Login(string email, string password);
        Task<Response<bool>> ChangePassword(int userId, ChangePasswordDTO dto);
        Task<Response<string>> UploadProfileImage(int userId, IFormFile file);
        Task<Response<bool>> UpdateUserProfile(int userId, ProfileDTO updateProfileDto);
        Task<bool> DeleteAccount(int userId);
    }
}