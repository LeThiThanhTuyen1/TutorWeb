using Microsoft.Extensions.Caching.Memory;

namespace TutorWebAPI.Services
{
    public class TokenBlacklistService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(30); 

        public TokenBlacklistService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void BlacklistToken(string token, TimeSpan? expiry = null)
        {
            var expiration = expiry ?? _defaultExpiry;
            _cache.Set(token, true, expiration);
        }

        public bool IsTokenBlacklisted(string token)
        {
            return _cache.TryGetValue(token, out _);
        }
    }
}
