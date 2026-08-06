using Souqify.Application.DTOs.Order;

namespace Souqify.Application.Interfaces
{
    public interface IOrderQueries
    {
        public Task<OrderDto?> GetOrderByIdAsync(Guid orderId,Guid userId);
        public Task<IEnumerable<OrderSummaryDto>> GetAllUserOrdersAsync(Guid userId);
        public Task<OrderDto?> GetOrderByIdempotencyKeyAsync(Guid idempotencyKey, Guid userId);
        public Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber,Guid userId);
    }
}
