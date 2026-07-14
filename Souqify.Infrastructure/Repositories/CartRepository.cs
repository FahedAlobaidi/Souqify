
using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public CartRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task AddCartAsync(Cart cart)
        {
            await _souqifyDbContext.Carts.AddAsync(cart);
        }

        public async Task CreateCart(Cart cart)
        {
            await _souqifyDbContext.Carts.AddAsync(cart);
        }

        public async Task<Cart?> GetCartAsync(Guid userId)
        {
            return await _souqifyDbContext.Carts.Where(c => c.UserId == userId).Include(c=>c.CartItems).FirstOrDefaultAsync();
        }

        public async Task DeleteCartAsync(Cart cart)
        {
            _souqifyDbContext.Carts.Remove(cart);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0; 
        }
    }
}
