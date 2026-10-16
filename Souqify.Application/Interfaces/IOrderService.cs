

using Souqify.Application.DTOs.Order;
using Souqify.Application.Models.BackgroundJob;
using Souqify.Domain.Entities.Enums;

namespace Souqify.Application.Interfaces
{
    public interface IOrderService
    {
        public Task<OrderDto> CreateOrderAsync(Guid userId,Guid idempotencyKey,CreateOrderDto createOrderDto);
        public Task HandlePaymentEventAsync(string rawBody, string signature);
        public Task<OrderDto> ResumePaymentAsync(Guid userId, Guid orderId);
        public Task<PaymentConfirmationOrderDto> ConfirmPaymentAsync(string sessionId,Guid userId);
        public Task<IEnumerable<OrderSummaryDto>> GetUserOrdersAsync(Guid userId);
        public Task<OrderDto> GetOrderByIdAsync(Guid orderId, Guid userId);
        public Task<StaleOrdersCancelationResult> CleanupStaleOrders(PaymentMethod paymentMethod, CancellationToken cancellationToken);
        public Task<OrderDto> GetOrderByOrderNumberAsync(string orderNumber, Guid userId);
    }
}
