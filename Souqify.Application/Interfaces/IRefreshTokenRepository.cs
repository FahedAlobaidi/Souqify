

using Souqify.Domain.Entities;

namespace Souqify.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        void AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        void UpdateTokenAsync(RefreshToken refreshToken);
        Task RevokAllForUserAsync(Guid userId);
        Task<bool> SaveChanges();
    }
}
