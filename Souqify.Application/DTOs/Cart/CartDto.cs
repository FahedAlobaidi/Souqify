

namespace Souqify.Application.DTOs.Cart
{
    public class CartDto
    {
        public Guid Id { get; set; }

        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();

        public decimal TotalPrice { get; set; }
    }
}
