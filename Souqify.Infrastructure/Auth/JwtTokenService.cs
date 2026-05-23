
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Souqify.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;


namespace Souqify.Infrastructure.Auth
{
    public class JwtTokenService : IJwtTokenService
    {

        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _securityKey;
        private readonly int _expirationInMinute;

        public JwtTokenService(IConfiguration configuration)
        {
            _issuer = configuration["Authentication:Issuer"]
                ?? throw new InvalidOperationException("Authentication:issuer not configured");
            _audience = configuration["Authentication:Audience"]
                ?? throw new InvalidOperationException("Authentication:audience not configured");
            _securityKey = configuration["Authentication:SecretKey"]
                ?? throw new InvalidOperationException("Authentication:secret key not configured");

            var expirationString = configuration["Authentication:ExpirationInMinutes"];
            if (expirationString != null)
            {
                _expirationInMinute = int.Parse(expirationString);
            }
            else
            {
                throw new InvalidOperationException("Authentication:ExpirationInMinutes not configured");
            }
        }

        public string GenerateAccessToken(Guid userId, string email, List<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub,userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email,email),
            };

            foreach(var item in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, item));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _issuer,
                Audience = _audience,
                Expires = DateTime.UtcNow.AddMinutes(_expirationInMinute),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(_securityKey)), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
