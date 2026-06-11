using Souqify.Domain.Entities;
using Microsoft.AspNetCore.Identity;


namespace Souqify.Infrastructure.Identity
{
    public class ApplicationUser:IdentityUser<Guid>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
