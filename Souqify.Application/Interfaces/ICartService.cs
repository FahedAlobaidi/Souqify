

using Souqify.Application.DTOs.Cart;

namespace Souqify.Application.Interfaces
{
    public interface ICartService
    {
        public Task<CartDto> GetGuestCartAsync(Guid guestId);
        public Task<CartDto> GetUserCartAsync(Guid userId);
        public Task<CartDto> AddGuestCartAsync(Guid guestId,CreateCartDto createCartDto);
        public Task<CartDto> AddUserCartAsync(Guid userId, CreateCartDto createCartDto);
        public Task<CartDto> DecreaseGuestCartItemQuantityAsync(Guid customerId, Guid cartItemId);
        public Task<CartDto> DeleteGuestCartAsync(Guid customerId);
        public Task<CartDto> DeleteUserCartAsync(Guid uderId);
        public Task<CartDto> DeleteGuestCartItemAsync(Guid customerId, Guid cartItemId);
        public Task<CartDto> DeleteUserCartItemAsync(Guid userId, Guid cartItemId);
        public Task<CartDto> DecreaseUserCartItemsQuantityAsync(Guid userId, Guid cartItemId);
        public Task MergeGuestCartAsync(Guid? guestId, Guid userId);
    }
}
