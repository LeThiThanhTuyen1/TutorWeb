using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private Dictionary<string, User> pendingUsers = new Dictionary<string, User>();
        private readonly IEmailService _emailService;
        private readonly ITutorService _tutorService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private readonly TokenBlacklistService _tokenBlacklistService;

        public AuthService(IAuthRepository authRepository, IEmailService emailService, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger, IMemoryCache cache, IConfiguration config, TokenBlacklistService tokenBlacklistService, ITutorService tutorService)
        {
            _authRepository = authRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _cache = cache;
            _config = config;
            _tutorService = tutorService;
            _tokenBlacklistService = tokenBlacklistService;
        }

        public async Task<bool> IsEmailExist(string email)
        {
            _logger.LogInformation("Kiểm tra tồn tại của email: {0}.", email);

            if (_cache.TryGetValue($"EmailExists_{email}", out bool emailExists))
            {
                return emailExists;
            }

            emailExists = await _authRepository.IsEmailExist(email);
            _cache.Set($"EmailExists_{email}", emailExists, TimeSpan.FromMinutes(10));
            return emailExists;
        }

        public async Task<User> GetUserByEmail(string email)
        {
            if (_cache.TryGetValue($"GetUserEmail_{email}", out User userEmailExists))
            {
                return userEmailExists;
            }

            userEmailExists = await _authRepository.GetUserByEmail(email);
            _cache.Set($"EmailExists_{email}", userEmailExists, TimeSpan.FromMinutes(10));
            return userEmailExists;
        }

        public async Task<Response<bool>> RegisterUser(RegisterDTO dto)
        {
            if (await IsEmailExist(dto.Email))
            {
                _logger.LogWarning("Đăng ký thất bại: Email {0} đã tồn tại.", dto.Email);
                return new Response<bool>
                {
                    Succeeded = false,
                    Message = "Email đã tồn tại.",
                    Errors = new string[] { "Email cung cấp đã tồn tại." },
                    Data = false
                };
            }

            string verificationCode = GenerateVerificationCode();

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = dto.Password,
                Phone = dto.Phone,
                Role = dto.Role,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                ProfileImage = "uploads/userprofile.jpg",
                School = dto.School,
                Location = dto.Location,
                isDeleted = false,
                Verified = false
            };

            _cache.Set($"VerificationCode_{dto.Email}", verificationCode, TimeSpan.FromMinutes(15));
            _cache.Set($"PendingUser_{dto.Email}", user, TimeSpan.FromMinutes(15));

            await _emailService.SendVerificationEmail(dto.Email, verificationCode);
            _logger.LogInformation("Gửi mã xác nhận đến {0}.", dto.Email);

            return new Response<bool>
            {
                Succeeded = true,
                Message = "Mã xác nhận đã được gửi đến email của bạn. Làm ơn hãy kiểm tra.",
                Data = true
            };
        }

        //public async Task<User> GetUserByRefreshToken(string refreshToken)
        //{
        //    return await _authRepository.GetUserByRefreshToken(refreshToken);
        //}

        public async Task<bool> UpdateUser(User user)
        {
            return await _authRepository.UpdateUser(user);
        }

        public async Task<Response<bool>> VerifyUser(string email, string code)
        {
            var response = new Response<bool>();

            if (!_cache.TryGetValue($"VerificationCode_{email}", out string? storedCode) || storedCode != code)
            {
                _logger.LogWarning("Xác minh {0} thất bại. Mã xác minh không đúng.", email);
                response.Succeeded = false;
                response.Message = "Mã xác minh không đúng.";
                response.Errors = new string[] { "Mã xác minh bạn nhập không đúng." };
                return response;
            }

            if (!_cache.TryGetValue($"PendingUser_{email}", out User? user))
            {
                _logger.LogWarning("Xác minh tài khoản {0} thất bại. Người dùng không tồn tại.", email);
                response.Succeeded = false;
                response.Message = "Người dùng không tồn tại.";
                response.Errors = new string[] { "Không người dùng nào được tìm thấy với email." };
                return response;
            }

            user.Verified = true;
            await _authRepository.RegisterUser(user);

            _cache.Remove($"VerificationCode_{email}");
            _cache.Remove($"PendingUser_{email}");

            _logger.LogInformation("Người dùng {0} đã xác minh thành công.", email);
            response.Succeeded = true;
            response.Message = "Người dùng đã xác minh thành công.";
            response.Data = true;

            return response;
        }

        public async Task<bool> ResendVerificationCode(string email)
        {
            if (pendingUsers.ContainsKey(email))
            {
                string verificationCode = GenerateVerificationCode();
                await _emailService.SendVerificationEmail(email, verificationCode);

                return true;
            }
            return false;
        }

        public async Task<bool> ForgotPassword(string email)
        {
            var user = await _authRepository.GetUserByEmail(email);
            if (user == null) return false;

            string verificationCode = GenerateVerificationCode();
            _cache.Set($"VerificationCode_{user.Email}", verificationCode, TimeSpan.FromMinutes(10));

            await _emailService.SendVerificationEmail(email, verificationCode);
            return true;
        }

        public async Task<Response<bool>> ResetPassword(ResetPasswordRequest request)
        {
            var response = new Response<bool>();

            if (!_cache.TryGetValue($"VerificationCode_{request.Email}", out string? storedCode) || storedCode != request.ResetCode)
            {
                _logger.LogWarning("Đặt lại mật khẩu thất bại: Mã xác minh không đúng.");
                response.Succeeded = false;
                response.Message = "Mã xác minh không đúng.";
                response.Errors = new string[] { "Mã xác minh bạn nhập không đúng." };
                return response;
            }

            var user = await _authRepository.GetUserByEmail(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Đặt lại mật khẩu thất bại: Người dùng với email {0} không tồn tại.", request.Email);
                response.Succeeded = false;
                response.Message = "Người dùng không tồn tại.";
                response.Errors = new string[] { "Người dùng không tồn tại." };
                return response;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _authRepository.UpdateUser(user);

            _cache.Remove($"VerificationCode_{request.Email}");
            _cache.Remove($"PendingUser_{request.Email}");

            _logger.LogInformation("Đặt lại mật khẩu thành công với email {0}.", request.Email);
            response.Succeeded = true;
            response.Message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.";
            response.Data = true;

            return response;
        }

        public async Task<Response<LoginResponse?>> Login(string email, string password)
        {
            var response = new Response<LoginResponse?>();

            if (!await _authRepository.ValidateUser(email, password))
            {
                _logger.LogWarning("Thông tin {Email} không hợp lệ.", email);
                response.Succeeded = false;
                response.Message = "Email hoặc mật khẩu sai.";
                return response;
            }

            var user = await _authRepository.GetUserByEmail(email);
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với email: {Email}", email);
                return new Response<LoginResponse?>
                {
                    Succeeded = false,
                    Message = "Không tìm thấy người dùng.",
                    Data = null
                };
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenString();

            await _authRepository.UpdateUser(user);

            var loginResponse = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            _logger.LogInformation("Đăng nhập thành công với email: {Email}", email);

            return new Response<LoginResponse?>
            {
                Succeeded = true,
                Message = "Đăng nhập thành công.",
                Data = loginResponse
            };
        }

        public async Task<Response<bool>> ChangePassword(int userId, ChangePasswordDTO dto)
        {
            var user = await _authRepository.GetProfileById(userId);
            if (user == null)
                return CreateErrorResponse("Người dunhg không tồn tại.", "Thông tin hoặc token không hợp lệ.");

            if (!await _authRepository.ValidateUser(user.Email, dto.OldPassword))
                return CreateErrorResponse("Mật khẩu hiện tại không chính xác.", "Mật khẩu cũ không chính xác.");

            if (dto.OldPassword == dto.NewPassword)
                return CreateErrorResponse("Mật khẩu mới phải khác mật khẩu cũ.", "Làm ơn nhập mật khẩu khác với mật khẩu hiện tại.");

            if (dto.ConfirmNewPassword != dto.NewPassword)
                return CreateErrorResponse("Mật khẩu mới nhập lại không trùng khớp.", "Mật khẩu mới nhập lại không trùng khớp với mật khẩu mới.");

            await _authRepository.UpdatePassword(userId, dto.NewPassword);
            _logger.LogInformation("Cập nhật mật khẩu thành công với user ID {0}.", userId);

            return new Response<bool>
            {
                Succeeded = true,
                Message = "Cập nhật mật khẩu thành công.",
                Data = true
            };
        }

        public async Task<Response<string>> UploadProfileImage(int userId, IFormFile file)
        {
            var user = await _authRepository.GetProfileById(userId);
            if (user == null)
            {
                _logger.LogWarning("Cập nhật ảnh thất bại: Không tìm thấy người dùng với ID {0}.", userId);
                return new Response<string>
                {
                    Succeeded = false,
                    Message = "Người dùng không tồn tại.",
                    Errors = new string[] { "Dữ liệu không hợp lệ." },
                    Data = null
                };
            }

            try
            {
                var imagePath = await _authRepository.UploadProfileImage(userId, file);
                if (string.IsNullOrEmpty(imagePath))
                {
                    return new Response<string>
                    {
                        Succeeded = false,
                        Message = "Không thể lưu ảnh.",
                        Errors = new string[] { "Có lỗi không mong đợi trong quá trình lưu ảnh." },
                        Data = null
                    };
                }

                _cache.Remove($"UserProfile_{userId}");
                return new Response<string>
                {
                    Succeeded = true,
                    Message = "Cập nhật ảnh thành công.",
                    Data = imagePath
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("Lỗi khi lưu ảnh: {0}", ex.Message);
                return new Response<string>
                {
                    Succeeded = false,
                    Message = ex.Message,
                    Errors = new string[] { "Dữ liệu không hợp lệ." },
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Cập nhật ảnh thất bại ví có lỗi xảy rar: {0}", ex.Message);
                return new Response<string>
                {
                    Succeeded = false,
                    Message = "Lỗi server.",
                    Errors = new string[] { ex.Message },
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> UpdateUserProfile(int userId, ProfileDTO updateProfileDto)
        {
            var user = await _authRepository.GetProfileById(userId);
            if (user == null)
            {
                _logger.LogWarning("Cập nhật ảnh thất bại: Không tìm thấy người dùng với ID {0}.", userId);
                return new Response<bool>
                {
                    Succeeded = false,
                    Message = "Người dùng không tồn tại.",
                    Errors = new string[] { "Thông tin người dùng không hợp lệ." },
                    Data = false
                };
            }

            var result = await _authRepository.UpdateProfile(userId, updateProfileDto);
            if (!result)
            {
                _logger.LogWarning("Không thể cập nhật ảnh với ID {0}.", userId);
                return new Response<bool>
                {
                    Succeeded = false,
                    Message = "Cập nhật ảnh thất bại.",
                    Errors = new string[] { "Có lỗi trong quá trình cập nhật ảnh hồ sơ." },
                    Data = false
                };
            }
            _tutorService.InvalidateTutorCache(userId);
            _cache.Remove($"UserProfile_{userId}");
            _logger.LogInformation("Cập nhật ảnh hồ sơ thành công với user ID {0}.", userId);
            return new Response<bool>
            {
                Succeeded = true,
                Message = "Hồ sơ được cập nhật thành công.",
                Data = result
            };
        }

        public async Task<(bool Success, string Message)> Logout()
        {
            if (!_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                _logger.LogWarning("Không tồn tại token. Đăng xuất thất bại.");
                return (false, "Token không tồn tại.");
            }

            var token = authHeader.ToString().Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("TOken không hợp lệ. Đăng xuất thất bại.");
                return (false, "Token không hợp lệ.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                _logger.LogWarning("Định dạng JWT không hợp lệ. Đăng xuất thất bại.");
                return (false, "Định dạng JWT không hợp lệ.");
            }

            var expiry = TimeSpan.FromMinutes(30);
            _tokenBlacklistService.BlacklistToken(token, expiry);

            return (true, "Đăng xuất thành công.");
        }

        public async Task RemoveRefreshToken(int userId)
        {
            await _authRepository.RemoveRefreshToken(userId);
        }

        public async Task<Response<ProfileDTO>> GetProfile(int userId)
        {
            if (_cache.TryGetValue($"UserProfile_{userId}", out ProfileDTO? cachedProfile))
            {
                _logger.LogInformation("Lấy thông tin người dùng với ID {0} từ bộ nhớ đệm.", userId);
                return new Response<ProfileDTO>
                {
                    Succeeded = true,
                    Message = "Lấy thông tin hồ sơ thành công.",
                    Data = cachedProfile
                };
            }

            var profile = await _authRepository.GetProfileById(userId);
            if (profile == null)
            {
                _logger.LogWarning("Có lỗi xảy ra trong quá trình lấy thông tin của người dùng ID {0}.", userId);
                return new Response<ProfileDTO>
                {
                    Succeeded = false,
                    Message = "Người dùng không tồn tại.",
                    Errors = new string[] { "Người dùng không tồn tại." },
                    Data = null
                };
            }

            _cache.Set($"UserProfile_{userId}", profile, TimeSpan.FromMinutes(30));

            return new Response<ProfileDTO>
            {
                Succeeded = true,
                Message = "Thông tin hồ sơ được lấy thành công.",
                Data = profile
            };
        }

        public async Task<bool> DeleteAccount(int userId)
        {
            return await _authRepository.DeleteAccount(userId);
        }

        private string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private Response<bool> CreateErrorResponse(string message, params string[] errors)
        {
            _logger.LogWarning(message);
            return new Response<bool>
            {
                Succeeded = false,
                Message = message,
                Errors = errors,
                Data = false
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException(nameof(jwtKey), "JWT Key is missing from configuration.");
            }
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "")
                };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(60),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
