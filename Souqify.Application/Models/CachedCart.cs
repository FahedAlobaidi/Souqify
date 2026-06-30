

using Souqify.Application.DTOs.Cart;

namespace Souqify.Application.Models
{
    public class CachedCart
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }
        public Guid? GuestId { get; set; }

        public List<CachedCartItems> CartItems { get; set; } = new List<CachedCartItems>();
    }
}
