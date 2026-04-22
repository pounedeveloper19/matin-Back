using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MatinPower.Infrastructure
{
    public class TokenManager
    {
        const string security = "401b09eab3c013d4ca54922bb802bec8fd5318192b0a75f201d8b3727429090fb337591abd3e44453b954555b7a0812e1081c39b740293f765eae731f5a65ed1";
        public static string CreateToken(string username, string locationId, string userId)
        {
            DateTime issuedAt = DateTime.Now;
            DateTime expires = DateTime.Now.AddDays(1);

            var tokenHandler = new JwtSecurityTokenHandler();

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.GroupSid, locationId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                // "sub" claim required by UseContext.GetUserName()
                new Claim(JwtRegisteredClaimNames.Sub, username),
            });
            var securityKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(security));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            //create the jwt
            var token = (JwtSecurityToken)tokenHandler.CreateJwtSecurityToken(issuer: "http://localhost:3000", audience: "http://localhost:3000", subject: claimsIdentity, notBefore: issuedAt, expires: expires, signingCredentials: signingCredentials);
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }
        public static string GetTokenUserName(string? token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(token)) return null;
            var tokenRead = handler.ReadToken(token) as JwtSecurityToken;
            var userName = tokenRead.Claims.FirstOrDefault()?.Value;
            return userName!;
        }
        public static string GetTokenUserId(string? token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(token)) return null;
            var tokenRead = handler.ReadToken(token) as JwtSecurityToken;
            var userName = tokenRead.Claims.ToList()[2].Value;
            return userName!;
        }
        public static string GetTokenLocationId(string? token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(token)) return null;
            var tokenRead = handler.ReadToken(token) as JwtSecurityToken;
            var locationId = tokenRead?.Claims.ToList()[1].Value;
            return locationId!;
        }
        public string ValidateJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(security);
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;
                var name = jwtToken.Claims.First(x => x.Type == "Name").Value;
                return name;
            }
            catch
            {
                return null;
            }
        }
    }
}
