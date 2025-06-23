using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.Services;

namespace TutorWebAPI.Middleware
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenBlacklistService _tokenBlacklistService;

        public JwtBlacklistMiddleware(RequestDelegate next, TokenBlacklistService tokenBlacklistService)
        {
            _next = next;
            _tokenBlacklistService = tokenBlacklistService;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token) && _tokenBlacklistService.IsTokenBlacklisted(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token đã hết hạn.");
                return;
            }

            await _next(context);
        }
    }
}
