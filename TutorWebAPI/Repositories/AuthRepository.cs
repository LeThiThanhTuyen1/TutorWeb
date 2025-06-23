using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;

namespace TutorWebAPI.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(ApplicationDbContext context, IWebHostEnvironment env, ILogger<AuthRepository> logger)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        public async Task<bool> IsEmailExist(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ProfileDTO?> GetProfileById(int userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            var profileDto = new ProfileDTO
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone ?? "Unknow",
                Email = user.Email,
                Image = user.ProfileImage,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role,
                School = user.School ?? "Unknow",
                Location = user.Location ?? "Unknow",
                Gender = user.Gender ?? "Unknow"
            };

            if (user.Role == "Tutor")
            {
                var tutorInfo = await _context.Tutors
                    .Where(t => t.UserId == userId)
                    .FirstOrDefaultAsync();

                if (tutorInfo != null)
                {
                    profileDto.TutorInfo = new TutorInfoDTO
                    {
                        Experience = tutorInfo.Experience ?? 0f,
                        Subjects = tutorInfo.Subjects,
                        Introduction = tutorInfo.Introduction,
                        Rating = tutorInfo.Rating
                    };
                }
            }

            if (user.Role == "Student")
            {
                var studentInfo = await _context.Students
                    .Where(t => t.UserId == userId)
                    .FirstOrDefaultAsync();

                if (studentInfo != null)
                {
                    profileDto.StudentInfo = new StudentInfoDTO
                    {
                        Class = studentInfo.Class
                    };
                }
            }

            return profileDto;
        }

        //public async Task<User> GetUserByRefreshToken(string refreshToken)
        //{
        //    return await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        //}

        public async Task RegisterUser(User user)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            if (user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                await _context.Students.AddAsync(new Student { UserId = user.Id });
            else if (user.Role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
                await _context.Tutors.AddAsync(new Tutor { UserId = user.Id });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateUser(User user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateProfile(int userId, ProfileDTO updatedProfile)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.Name = updatedProfile.Name ?? user.Name;
            user.Phone = updatedProfile.Phone ?? user.Phone;
            user.Email = updatedProfile.Email ?? user.Email;
            user.DateOfBirth = updatedProfile.DateOfBirth ?? user.DateOfBirth;
            user.School = updatedProfile.School ?? user.School;
            user.Location = updatedProfile.Location ?? user.Location;
            user.Gender = updatedProfile.Gender ?? user.Gender;

            if (user.Role == "Tutor")
            {
                var tutorInfo = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tutorInfo != null)
                {
                    tutorInfo.Experience = updatedProfile.TutorInfo?.Experience ?? tutorInfo.Experience;
                    tutorInfo.Subjects = updatedProfile.TutorInfo?.Subjects ?? tutorInfo.Subjects;
                    tutorInfo.Introduction = updatedProfile.TutorInfo?.Introduction ?? tutorInfo.Introduction;
                }
            }

            if (user.Role == "Student")
            {
                var studentInfo = await _context.Students.FirstOrDefaultAsync(t => t.UserId == userId);
                if (studentInfo != null)
                {
                    studentInfo.Class = updatedProfile.StudentInfo?.Class ?? studentInfo.Class;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<int> GetUserIdByTutorId(int tutorId)
        {
            return await _context.Tutors
                .Where(t => t.Id == tutorId)
                .Select(t => t.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<Student> GetStudentByUserIdAsync(int userId)
        {
            try
            {
                return await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching the student. Please try again later.");
            }
        }

        public async Task<string?> UploadProfileImage(int userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Photo size must not exceed 5MB.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Only image formats supported (.jpg, .jpeg, .png, .gif, .webp).");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            user.ProfileImage = $"uploads/{uniqueFileName}";

            await _context.SaveChangesAsync();

            return user.ProfileImage;
        }

        public async Task<bool> ValidateUser(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && BCrypt.Net.BCrypt.Verify(password, user.Password);
        }

        public async Task<bool> UpdatePassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 10)
                throw new ArgumentException("Password must has 10 characters");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task RemoveRefreshToken(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                //user.RefreshToken = "";
                //user.RefreshTokenExpiry = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteAccount(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting user with ID {UserId}", userId);
                throw new Exception("An error occurred while deleting the account. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting user with ID {UserId}", userId);
                throw new Exception("An unexpected error occurred. Please try again.");
            }
        }
    }
}
