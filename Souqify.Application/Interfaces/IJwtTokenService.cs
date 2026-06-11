

namespace Souqify.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(Guid userId, string email, List<string> roles);
        string GenerateRefreshToken();
    }
}
