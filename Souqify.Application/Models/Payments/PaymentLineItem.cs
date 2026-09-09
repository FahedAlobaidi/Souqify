namespace Souqify.Application.Models.Payments
{
    public class PaymentLineItem
    {
        public int Quantity { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Variant { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}
