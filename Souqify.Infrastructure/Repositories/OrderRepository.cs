
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Order = Souqify.Domain.Entities.Order;

namespace Souqify.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public OrderRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task CreateOrderAsync(Order order)
        {
            await _souqifyDbContext.Orders.AddAsync(order);
        }

        public async Task<Order?> GetOrderBySessionIdAsync(string sessionId)
        {
            return await _souqifyDbContext.Orders.Where(o => o.PaymentSessionId == sessionId).Include(o=>o.Items).FirstOrDefaultAsync();
        }

        public async Task<Order?> GetOrderEntById(Guid orderId,Guid userId)
        {
           return await _souqifyDbContext.Orders.Where(o => o.Id == orderId && o.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            try
            {
                return await _souqifyDbContext.SaveChangesAsync() > 0;

            }catch(DbUpdateException ex) when(ex.InnerException is PostgresException { SqlState: "23505" } pg && pg.ConstraintName == "IX_Orders_IdempotencyKey")
            {
                throw new DuplicateIdempotencyKeyException("This order was already submitted");
            }
                
        }
    }
}
