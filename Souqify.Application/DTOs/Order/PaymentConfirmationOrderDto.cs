
namespace Souqify.Application.DTOs.Order
{
    public class PaymentConfirmationOrderDto
    {
        public string? OrderNumber { get; set; }
        public decimal TotalAmount { get; set; } = 0;
        public string Currency { get; set; } = "USD";
        public DateTime Date { get; set; }
        public string ConfirmationStatus { get; set; } = null!;
    }
}
