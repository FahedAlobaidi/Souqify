

namespace Souqify.Domain.Entities
{
    public class CartItem
    {
        public Guid Id { get;private set; }

        public Guid ProductId { get;private set; }

        public Guid ProductVariantId { get;private set; }

        public Guid CartId { get;private set; }

        public int Quantity { get;private set; }

        public decimal PriceAtAdded { get;private set; }

        public DateTime AddedAt { get;private set; }

        public Product? Product { get; set; }

        public Cart? Cart { get; set; }

        public uint RowVersion { get; private set; }

        public ProductVariant? ProductVariant { get; set; }

        private CartItem() { }

        public CartItem(Guid productId, Guid variantId, Guid cartId, int quantity, decimal priceAtAdded)
        {
            if (quantity <= 0) throw new ArgumentException("Domain : quantity cant be less than or equal 0");
            if (priceAtAdded < 0) throw new ArgumentException("Domain : priceAtAdded cant be less than 0");

            ProductId = productId;
            ProductVariantId = variantId;
            CartId = cartId;
            Quantity = quantity;
            PriceAtAdded = priceAtAdded;
            AddedAt = DateTime.UtcNow;
        }

        public void IncreaseQuantity(int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Domain : quantity cant be less than or equal 0");
            Quantity = Quantity + quantity;
        }

        public void DecreaseQuantity()
        {
            if (Quantity <= 1)
                throw new InvalidOperationException("Use RemoveItem to remove; quantity cannot go below 1.");

            Quantity--;

        }
    }
}
