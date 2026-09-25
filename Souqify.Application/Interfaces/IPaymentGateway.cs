
using Souqify.Application.DTOs.Order;
using Souqify.Application.Models.Payments;

namespace Souqify.Application.Interfaces
{
    public interface IPaymentGateway
    {
        public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid orderId,decimal amount, string currency, List<PaymentLineItem> paymentLineItemList);
        public Task<CheckoutSessionResult> GetSessionBySessionIdAsync(string sessionId);
        public Task<PaymentEvent> ParseWebhookEventAsync(string rawBody,string signature);
        public Task<CheckoutSessionStatus> GetPaymentStatusAsync(string sessionId);
    }
}
