

namespace Souqify.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; private set; }
        public Guid OrderId { get; private set; }

        public Guid ProductVariantId { get; private set; }   // reference — for fulfilment
        public Guid ProductId { get; private set; }

        public string ProductNameSnapshot { get; private set; } = null!;  // frozen
        public string? VariantSnapshot { get; private set; }              // frozen: "Black / XL"
        public string? ProductImageSnapshot { get; private set; }         // frozen image url

        public decimal UnitPrice { get; private set; }   // frozen: BasePrice + PriceAdjustment
        public int Quantity { get; private set; }
        public decimal LineTotal { get; private set; }   // frozen: UnitPrice * Quantity

        public uint RowVersion { get; private set; }

        public Order? Order { get; set; }

        private OrderItem() { }  // EF

        public OrderItem(Guid orderId, Guid productId, Guid productVariantId,string productNameSnapshot, string? variantSnapshot, string? productImageSnapshot,decimal unitPrice, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than 0");
            if (unitPrice < 0) throw new ArgumentException("UnitPrice cannot be negative");

            Id = Guid.NewGuid();
            OrderId = orderId;
            ProductId = productId;
            ProductVariantId = productVariantId;
            ProductNameSnapshot = productNameSnapshot;
            VariantSnapshot = variantSnapshot;
            ProductImageSnapshot = productImageSnapshot;
            UnitPrice = unitPrice;
            Quantity = quantity;
            LineTotal = unitPrice * quantity;   // frozen at creation
        }
    }
}
