

namespace Souqify.Application.DTOs.Cart
{
    public class CartDto
    {
        public Guid Id { get; set; }

        public List<CartItemsDto> CartItems { get; set; } = new List<CartItemsDto>();

        public decimal TotalPrice { get; set; }
    }
}
