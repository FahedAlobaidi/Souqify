

namespace Souqify.Application.DTOs.Cart
{
    public class UpdateCartItemDto
    {
        public Guid VariantId { get; set; }

        public int Quantity { get; set; }
    }
}
