using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authentication;

namespace TicketManagement.Infrastructure
{
    public class UseContext
    {
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UseContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public string? GetTokenHeader()
        {
            var accessToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
            if (accessToken == "null")
                return null;
            if (!string.IsNullOrEmpty(accessToken))
            {
                string token = accessToken;

                if (token.StartsWith("\""))
                {
                    token = token.Substring(1);
                }

                if (token.EndsWith("\""))
                {
                    token = token.Substring(0, token.Length - 1);
                }
                return token;
            }
            return null;
        }

        public string GetUserIp()
        {
            var ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;
            return ip.ToString();
        }
        public string GetUserAgent()
        {
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"];
            return userAgent;
        }
        public string GetUserUrl()
        {
            var url = _httpContextAccessor.HttpContext.Request.Host.Value;
            return url;
        }

        public string? GetUserName()
        {
            string? token = GetTokenHeader();
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userName = jwtToken.Claims.First(claim => claim.Type == "sub").Value;
                if (userName.Contains("@"))
                    userName = userName.Substring(0, userName.IndexOf("@"));
                return userName;
            }
            return null;
        }
        public int? GetUserId()
        {
            var userName = GetUserName();
            if (string.IsNullOrEmpty(userName))
                return null;

            var cacheKey = $"userId_{userName}";
            if (_cache.TryGetValue(cacheKey, out int? cachedId))
                return cachedId;

            var user = Repository<User>.GetLast(i => i.Mobile == userName);
            var userId = user?.Id;
            _cache.Set(cacheKey, userId, TimeSpan.FromMinutes(5));
            return userId;
        }
        public int? GetCustomerId()
        {
            var userName = GetUserName();
            if (string.IsNullOrEmpty(userName))
                return null;

            // Use a separate cache key from GetUserId() to avoid collision
            var cacheKey = $"customerId_{userName}";
            if (_cache.TryGetValue(cacheKey, out int? cachedId))
                return cachedId;

            var user = Repository<User>.GetLast(i => i.Mobile == userName);
            var customerId = user?.CustomerProfileId;
            _cache.Set(cacheKey, customerId, TimeSpan.FromMinutes(5));
            return customerId;
        }

        public int? GetAddressId()
        {
            var customerId = GetCustomerId();
            // Bug fix: return null when customerId is missing, not when it has a value
            if (!customerId.HasValue)
                return null;
            var customer = Repository<CustomerProfile>.GetLast(i => i.Id == customerId);
            var addressId = customer?.AddressId;
            return addressId;
        }

        public string? GetFullName()
        {
            var userId = GetUserId();
            // Bug fix: check HasValue instead of comparing int? to 0
            if (userId.HasValue)
            {
                var user = Repository<User>.GetLast(i => i.Id == userId);
                return user?.FullName;
            }
            return null;
        }
    }
}
