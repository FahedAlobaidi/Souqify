
using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Address;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
using StackExchange.Redis;
using System.Linq.Expressions;

namespace Souqify.Infrastructure.Queries
{
    public class OrderQueries : IOrderQueries
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public OrderQueries(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetAllUserOrdersAsync(Guid userId)
        {
            return await _souqifyDbContext.Orders.AsNoTracking().Where(o=>o.UserId == userId).Select(o=> new OrderSummaryDto
            {
                Id= o.Id,
                OrderNumber= o.OrderNumber,
                Status=o.Status.ToString(),
                PaymentStatus=o.PaymentStatus.ToString(),
                PaymentMethod=o.PaymentMethod.ToString(),
                TotalAmount=o.TotalAmount,
                CreatedAt=o.CreatedAt,
                Currency=o.Currency,
                
            }).OrderByDescending(o=>o.CreatedAt).ToListAsync();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, Guid userId)
        {
            return await _souqifyDbContext.Orders.Where(o => o.Id == orderId && o.UserId == userId).Select(ToOrderDto).FirstOrDefaultAsync();
        }

        public async Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber, Guid userId)
        {
            return await _souqifyDbContext.Orders.Where(o=>o.OrderNumber==orderNumber&&o.UserId==userId).Select(ToOrderDto).FirstOrDefaultAsync();
        }

        public async Task<OrderDto?> GetOrderByIdempotencyKeyAsync(Guid idempotencyKey, Guid userId)
        {
            return await _souqifyDbContext.Orders.Where(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId).Select(ToOrderDto).FirstOrDefaultAsync();
        }

        private static readonly Expression<Func<Domain.Entities.Order, OrderDto>> ToOrderDto = o => new OrderDto
        {


            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            PaymentMethod = o.PaymentMethod.ToString(),
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            Currency = o.Currency,
            ContactPhone = o.ContactPhone,
            Subtotal = o.Subtotal,
            ShippingCost = o.ShippingCost,
            ShippingAddress = new AddressDto
            {
                Street = o.ShippingAddress.Street,
                City = o.ShippingAddress.City,
                Region = o.ShippingAddress.Region,
                PostalCode = o.ShippingAddress.PostalCode,
                DeliveryNote = o.ShippingAddress.DeliveryNote,
            },
            Items = o.Items.OrderBy(oi => oi.Id).Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductVariantId = oi.ProductVariantId,
                Variant = oi.VariantSnapshot,
                UnitPrice = oi.UnitPrice,
                ImageUrl = oi.ProductImageSnapshot,
                ProductName = oi.ProductNameSnapshot,
                Quantity = oi.Quantity,
                LineTotal = oi.LineTotal,
            }).ToList()
        };

    }
}
