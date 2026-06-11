

namespace Souqify.Domain.Entities
{
    public class CartItem
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public Guid CartId { get; set; }

        public int Quantity { get; set; }

        public decimal PriceAtAdd { get; set; }

        public DateTime AddedAt { get; set; }

        public Product? Product { get; set; }

        public Cart? Cart { get; set; }

        public ProductVariant? ProductVariant { get; set; }
    }
}
