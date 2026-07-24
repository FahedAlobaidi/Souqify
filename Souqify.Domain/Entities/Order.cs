using Souqify.Domain.Entities.Enums;
using Souqify.Domain.Entities.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; private set; } = null!;   // assigned by app layer
        public Guid UserId { get; private set; }

        public OrderStatus Status { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }

        public decimal Subtotal { get; private set; }
        public decimal ShippingCost { get; private set; }
        public decimal TotalAmount { get; private set; }
        public string Currency { get; private set; } = "JOD";

        public Address ShippingAddress { get; private set; } = null!;  // owned, frozen
        public string ContactPhone { get; private set; } = null!;

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items;

        public uint RowVersion { get; private set; }

        private Order() { }  // EF

        public Order(Guid userId, Address shippingAddress, string contactPhone,
                     PaymentMethod paymentMethod, decimal shippingCost)
        {
            if (userId == Guid.Empty) throw new ArgumentException("Order must have an owner");
            if (string.IsNullOrWhiteSpace(contactPhone)) throw new ArgumentException("Contact phone is required");
            if (shippingCost < 0) throw new ArgumentException("Shipping cost cannot be negative");

            Id = Guid.NewGuid();
            UserId = userId;
            ShippingAddress = shippingAddress;
            ContactPhone = contactPhone;
            PaymentMethod = paymentMethod;
            ShippingCost = shippingCost;

            Status = OrderStatus.Pending;
            PaymentStatus = PaymentStatus.Unpaid;
            Currency = "JOD";
            CreatedAt = DateTime.UtcNow;
        }

        // ── building the order ──
        public void AddItem(OrderItem item)
        {
            _items.Add(item);
            RecalculateTotals();
        }

        public void AssignOrderNumber(string orderNumber)
        {
            if (!string.IsNullOrWhiteSpace(OrderNumber))
                throw new DomainException("Order number is already assigned");
            OrderNumber = orderNumber;
        }

        // ── status guards (only legal transitions) ──
        public void Confirm()
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Only a pending order can be confirmed");
            Status = OrderStatus.Confirmed;
            Touch();
        }

        public void Ship()
        {
            if (Status != OrderStatus.Confirmed)
                throw new DomainException("Only a confirmed order can be shipped");
            Status = OrderStatus.Shipped;
            Touch();
        }

        public void Deliver()
        {
            if (Status != OrderStatus.Shipped)
                throw new DomainException("Only a shipped order can be delivered");
            Status = OrderStatus.Delivered;
            Touch();
        }

        public void Cancel()
        {
            if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
                throw new DomainException("Cannot cancel an order that has shipped or completed");

            if (PaymentStatus == PaymentStatus.Paid)
                throw new DomainException("Cannot cancel a paid order — refund it first");
            Status = OrderStatus.Cancelled;
            Touch();
        }

        // ── payment guards ──
        public void MarkPaid()
        {
            if(Status==OrderStatus.Cancelled)
                throw new DomainException("Cannot pay for a cancelled order");

            if (PaymentStatus == PaymentStatus.Paid)
                throw new DomainException("Order is already paid");
            PaymentStatus = PaymentStatus.Paid;
            Touch();
        }

        public void MarkRefunded()
        {
            if (PaymentStatus != PaymentStatus.Paid)
                throw new DomainException("Only a paid order can be refunded");
            PaymentStatus = PaymentStatus.Refunded;
            Touch();
        }

        // ── internal ──
        private void RecalculateTotals()
        {
            Subtotal = _items.Sum(i => i.LineTotal);
            TotalAmount = Subtotal + ShippingCost;
        }

        private void Touch() => UpdatedAt = DateTime.UtcNow;
    }
}
