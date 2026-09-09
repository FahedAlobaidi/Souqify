using Souqify.Domain.Entities;


namespace Souqify.Application.Interfaces
{
    public interface IOrderRepository
    {
        public Task CreateOrderAsync(Order order);
        public Task<Order?> GetOrderEntById(Guid orderId, Guid userId);
        public Task<Order?> GetOrderBySessionIdAsync(string sessionId);
        public Task<bool> SaveChangesAsync();
    }
}
