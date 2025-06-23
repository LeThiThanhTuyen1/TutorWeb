using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clarifaiApiKey = "2fa19aaf59af4c2bb65906210fb10755";
    public class ImageRequest
    {
        public string Image { get; set; }
    }

    public AuthController(IAuthService authService, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AuthController> logger)
    {
        _authService = authService;
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Check NSFW image
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("check-image")]
    public async Task<IActionResult> CheckImage([FromBody] ImageRequest request)
    {
        Console.WriteLine($"Received Image Base64: {request.Image.Substring(0, 50)}...");

        if (string.IsNullOrEmpty(request?.Image))
        {
            return BadRequest(new { error = "No image provided" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Key {_clarifaiApiKey}");

            var requestBody = new
            {
                inputs = new[]
                {
                new
                {
                    data = new
                    {
                        image = new
                        {
                            base64 = request.Image
                        }
                    }
                }
            }
            };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "https://api.clarifai.com/v2/models/d16f390eb32cad478c7ae150069bd2c6/outputs",
                jsonContent
            );

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Clarifai Response: {jsonResponse}");
            dynamic data = JsonConvert.DeserializeObject(jsonResponse);

            var concepts = data.outputs[0].data.concepts;
            if (concepts == null || concepts.Count == 0)
            {
                return Ok(new { isNSFW = false, nsfwScore = 0.0 });
            }

            var nsfwScore = 0.0;
            foreach (var concept in concepts)
            {
                Console.WriteLine($"Concept: {concept.name}, Value: {concept.value}");
                if (concept.name == "nsfw" || concept.name == "violence" || concept.name == "gore")
                {
                    nsfwScore = Math.Max(nsfwScore, (double)concept.value); 
                }
            }
            return Ok(new { isNSFW = nsfwScore > 0.0001, nsfwScore = nsfwScore });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to call Clarifai API", details = ex.Message });
        }
    }

    /// <summary>
    /// Registers a new user and sends an email verification code.
    /// </summary>
    /// <param name="dto">Contains registration information like email, password, and user details.</param>
    /// <returns>Success message indicating that the email has been sent for verification.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest("Dữ liệu không hợp lệ.");

        var result = await _authService.RegisterUser(dto);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Message = result.Message,
                Errors = result.Errors
            });
        }

        return Ok(result.Message);
    }

    /// <summary>
    /// Verifies a user's email using a code sent to their email address.
    /// </summary>
    /// <param name="dto">Contains the email and verification code.</param>
    /// <returns>Success message indicating the email has been verified or an error message.</returns>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDTO dto)
    {
        _logger.LogInformation("Xác minh email: {Email}", dto.Email);

        var result = await _authService.VerifyUser(dto.Email, dto.Code);

        return result.Succeeded
            ? Ok(new Response<string> { Succeeded = true, Message = result.Message })
            : BadRequest(new Response<string> { Succeeded = false, Message = result.Message, Errors = result.Errors });
    }

    /// <summary>
    /// Resends the email verification code to the specified email.
    /// </summary>
    /// <param name="dto">Contains the email for resending the verification code.</param>
    /// <returns>Success message indicating that a new verification code has been sent.</returns>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDTO dto)
    {
        _logger.LogInformation("Gửi mã lại email: {Email}", dto.Email);
        var success = await _authService.ResendVerificationCode(dto.Email);
        if (!success)
        {
            _logger.LogWarning("Lỗi khi gửi mã đến email: {Email}", dto.Email);
            return NotFound("Email không tồn tại hoặc đã được xác nhận.");
        }
        return Ok("Mã đã được gửi.");
    }

    /// <summary>
    /// Sends a password reset code to the specified email.
    /// </summary>
    /// <param name="request">Contains the email to send the reset code to.</param>
    /// <returns>Success message indicating that the reset code has been sent.</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        _logger.LogInformation("Yêu cầu đổi mật khẩu cho email: {Email}", request.Email);
        var success = await _authService.ForgotPassword(request.Email);
        if (!success)
        {
            _logger.LogWarning("Email không hợp lệ: {Email}", request.Email);
            return NotFound("Email không tồn tại.");
        }
        return Ok("Mã xác minh đã được gửi đến email của bạn.");
    }

    /// <summary>
    /// Resets the user's password after verifying the reset code.
    /// </summary>
    /// <param name="request">Contains the email, reset code, and new password information.</param>
    /// <returns>Success message if the password was reset or an error message.</returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        _logger.LogInformation("Đặt lại mật khẩu cho email: {Email}", request.Email);

        var result = await _authService.ResetPassword(request);

        return result.Succeeded
            ? Ok(new Response<string> { Succeeded = true, Message = result.Message })
            : BadRequest(new Response<string> { Succeeded = false, Message = result.Message, Errors = result.Errors });
    }

    /// <summary>
    /// Logs in the user and generates a JWT token for authentication.
    /// </summary>
    /// <param name="request">Contains the email and password for login.</param>
    /// <returns>A JWT token if the login is successful or an error message.</returns>
    /// <summary>
    /// Logs out the user by invalidating the current JWT token.
    /// </summary>
    /// <returns>Jwt Authorization.</returns>
    /// <returns>Jwt Authorization.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Người dùng đăng nhập: {Email}", request.Email);

        var loginResponse = await _authService.Login(request.Email, request.Password);

        if (!loginResponse.Succeeded || loginResponse.Data == null)
        {
            _logger.LogWarning("Đăng nhập thất bại với email: {Email}", request.Email);
            return BadRequest(new Response<string>
            {
                Succeeded = false,
                Message = loginResponse.Message,
                Errors = loginResponse.Errors
            });
        }

        return Ok(new Response<LoginResponse>
        {
            Succeeded = true,
            Message = loginResponse.Message,
            Data = loginResponse.Data
        });
    }



    /// <summary>
    /// Refresh token
    /// </summary>
    /// <param name="tokenResponse"></param>
    /// <returns>New access token and new refresh code to continue</returns>
    // [HttpPost("refresh")]
    // public async Task<IActionResult> Refresh([FromBody] TokenResponse tokenResponse)
    // {
    //     if (tokenResponse is null)
    //     {
    //         return BadRequest("Yêu cầu không hợp lệ.");
    //     }

    //     var user = await _authService.GetUserByRefreshToken(tokenResponse.RefreshToken);
    //     if (user == null || user.RefreshTokenExpiry < DateTime.Now)
    //     {
    //         return Unauthorized("Invalid or expired refresh token.");
    //     }

    //     var newAccessToken = GenerateJwtToken(user);
    //     var newRefreshToken = GenerateRefreshTokenString();

    //     user.RefreshToken = newRefreshToken;
    //     user.RefreshTokenExpiry = DateTime.Now.AddHours(12);

    //     await _authService.UpdateUser(user);

    //     return Ok(new
    //     {
    //         AccessToken = newAccessToken,
    //         RefreshToken = newRefreshToken
    //     });
    // }

    /// <summary>
    /// Changes the user's password after validating the old password.
    /// </summary>
    /// <param name="dto">Contains old and new passwords.</param>
    /// <returns>Success message if the password is changed, or an error message.</returns>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        _logger.LogInformation("Người dùng {UserId} đang thay đổi mật khẩu", userId);

        var result = await _authService.ChangePassword(userId, dto);

        return result.Succeeded
            ? Ok(new { Message = result.Message })
            : BadRequest(new { Message = result.Message, Errors = result.Errors });
    }

    /// <summary>
    /// Retrieves the profile of the currently logged-in user.
    /// </summary>
    /// <returns>Profile information of the current user.</returns>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _authService.GetProfile(userId);

        if (result.Succeeded)
        {
            return Ok(result.Data);
        }

        return Unauthorized(result.Message);
    }

    /// <summary>
    /// Update the profile of user
    /// </summary>
    /// <param name="updateProfileDto">Contains infomation of user with role</param>
    /// <returns>Success message if update successful, or an error message.</returns>
    [Authorize]
    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileDTO updateProfileDto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        _logger.LogInformation("Người dùng với ID {0} đang cập nhật thông tin cá nhân.", userId);

        var result = await _authService.UpdateUserProfile(userId, updateProfileDto);

        if (!result.Succeeded)
        {
            if (result.Message == "Không tìm thấy người dùng.")
            {
                return NotFound(new { Message = result.Message, Errors = result.Errors });
            }
            return BadRequest(new { Message = result.Message, Errors = result.Errors });
        }

        return Ok(new { Message = result.Message, Data = result.Data });
    }

    /// <summary>
    /// Upload image file 
    /// </summary>
    /// <param name="file"></param>
    /// <returns>Success message if upload successful with path of file, or an error message </returns>
    [Authorize]
    [HttpPost("upload-profile-image")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        _logger.LogInformation("Người dùng với ID {0} đang cập nhật ảnh hồ sơ.", userId);

        var result = await _authService.UploadProfileImage(userId, file);

        if (result.Succeeded)
        {
            return Ok(new { Message = result.Message, ProfileImage = result.Data });
        }

        return BadRequest(new { Message = result.Message, Errors = result.Errors });
    }

    /// <summary>
    /// Logout from system
    /// </summary>
    /// <returns>Message</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var result = await _authService.Logout();
        // await _authService.RemoveRefreshToken(userId);
        _logger.LogInformation("Người dùng đang đăng xuất.");

        return result.Success ? Ok(result.Message) : BadRequest(result.Message);
    }

    /// <summary>
    /// Delete user account based on id token
    /// </summary>
    /// <returns>Delete result</returns>
    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var success = await _authService.DeleteAccount(userId);
        if (!success)
            return NotFound(new { message = "Không tìm thấy người dùng." });

        return Ok(new Response<string>
        {
            Succeeded = true,
            Message = "Xóa tài khoản người dùng thành công."
        });
    }

    /// <summary>
    /// Generates a JWT token based on the user's information.
    /// </summary>
    /// <param name="user">The user to generate the token for.</param>
    /// <returns>A JWT token string.</returns>
    //private string GenerateJwtToken(User user)
    //{
    //    var jwtKey = _config["Jwt:Key"];
    //    if (string.IsNullOrEmpty(jwtKey))
    //    {
    //        throw new ArgumentNullException(nameof(jwtKey), "JWT Key is missing from configuration.");
    //    }
    //    var key = Encoding.UTF8.GetBytes(jwtKey);

    //    var securityKey = new SymmetricSecurityKey(key);
    //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    //    var claims = new List<Claim>
    //            {
    //                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    //                new Claim(ClaimTypes.Name, user.Name ?? ""),
    //                new Claim(ClaimTypes.Role, user.Role ?? "")
    //            };

    //    var token = new JwtSecurityToken(
    //        issuer: _config["Jwt:Issuer"],
    //        audience: _config["Jwt:Audience"],
    //        claims: claims,
    //        expires: DateTime.UtcNow.AddHours(60),
    //        signingCredentials: credentials
    //    );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}

    //private string GenerateRefreshTokenString()
    //{
    //    var randomNumber = new byte[64];
    //    using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(randomNumber);
    //    return Convert.ToBase64String(randomNumber);
    //}
}
