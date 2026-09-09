namespace Souqify.Application.Models.Payments
{
    public class PaymentEvent
    {
        public string SessionId { get; set; } = null!;
        public PaymentEventType EventType { get; set; }
    }
}
