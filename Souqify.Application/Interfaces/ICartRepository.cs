
using Souqify.Domain.Entities;

namespace Souqify.Application.Interfaces
{
    public interface ICartRepository
    {
        public Task<Cart?> GetCartAsync(Guid userId);
        public Task CreateCart(Cart cart);
        public Task AddCartAsync(Cart cart);
        public Task DeleteCartAsync(Cart cart);
        public Task<bool> SaveChangesAsync(); 
    }
}
