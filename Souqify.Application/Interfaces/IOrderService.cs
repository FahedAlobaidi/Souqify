

using Souqify.Application.DTOs.Order;

namespace Souqify.Application.Interfaces
{
    public interface IOrderService
    {
        public Task<OrderDto> CreateOrderAsync(Guid userId,Guid idempotencyKey,CreateOrderDto createOrderDto);
        public Task<IEnumerable<OrderSummaryDto>> GetUserOrdersAsync(Guid userId);
        public Task<OrderDto> GetOrderByIdAsync(Guid orderId, Guid userId);
        public Task<OrderDto> GetOrderByOrderNumberAsync(string orderNumber, Guid userId);
    }
}
