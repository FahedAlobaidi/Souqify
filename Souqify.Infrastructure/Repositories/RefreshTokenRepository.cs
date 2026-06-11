

using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public RefreshTokenRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public void AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            _souqifyDbContext.RefreshTokens.Add(refreshToken);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            var refreshToken = await _souqifyDbContext.RefreshTokens.Where(rf => rf.Token == token).FirstOrDefaultAsync();
            return refreshToken;
        }

        public async Task RevokAllForUserAsync(Guid userId)
        {
            var userTokens = await _souqifyDbContext.RefreshTokens.Where(rf => rf.UserId == userId).ToListAsync();

            foreach(var item in userTokens)
            {
                item.RevokedAt = DateTime.UtcNow;
            }
        }

        public void UpdateTokenAsync(RefreshToken refreshToken)
        {
            _souqifyDbContext.RefreshTokens.Update(refreshToken);
        }

        public async Task<bool> SaveChanges()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0;
        }
    }
}
