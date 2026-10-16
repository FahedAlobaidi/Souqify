using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;


namespace Souqify.Application.Interfaces
{
    public interface IOrderRepository
    {
        public Task CreateOrderAsync(Order order);
        public Task<Order?> GetOrderEntById(Guid orderId, Guid userId);
        public Task<Order?> GetOrderBySessionIdAsync(string sessionId);
        public Task<List<Order>> GetStaleOrdersAsync(DateTime paymentDeadline, PaymentMethod paymentMethod, int batchSize, CancellationToken cancellationToken);
        public Task<bool> SaveChangesAsync();
    }
}
