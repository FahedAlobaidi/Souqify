

namespace Souqify.Application.DTOs.Cart
{
    public class CreateCartItemDto
    {
        public Guid ProductId { get; set; }

        public Guid VariantId { get; set; }

        public int Quantity { get; set; }
    }
}
