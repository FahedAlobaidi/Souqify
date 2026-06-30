

using Souqify.Application.DTOs.Cart;

namespace Souqify.Application.Interfaces
{
    public interface ICartService
    {
        public Task<CartDto> GetCartAsync(Guid customerId,bool isGuest);
        public Task<CartDto> AddCartAsync(Guid customerId,CreateCartDto createCartDto);
        public Task<CartDto> DecreaseCartItemQuantityAsync(Guid customerId, Guid cartItemId);
        public Task<CartDto> DeleteCartAsync(Guid customerId);
        public Task<CartDto> DeleteCartItemAsync(Guid customerId, Guid cartItemId);
    }
}
