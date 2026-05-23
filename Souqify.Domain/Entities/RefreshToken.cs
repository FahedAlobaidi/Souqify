namespace Souqify.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public string Token { get; set; } = null!;

        public Guid UserId { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? ReplacedByToken { get; set; }

        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
    }
}
