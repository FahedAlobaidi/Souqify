namespace Souqify.Application.Models.Payments
{
    public class CheckoutSessionResult
    {
        public string SessionId { get; set; } = null!;
        public string CheckoutUrl { get; set; } = null!;
    }
}
