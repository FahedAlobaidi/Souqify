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
        public Guid? IdempotencyKey { get;private set; }
        public bool HasStockReservation { get; private set; } = true;
        public string? PaymentSessionId { get;private set; }

        public OrderStatus Status { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }

        public decimal Subtotal { get; private set; }
        public decimal ShippingCost { get; private set; }
        public decimal TotalAmount { get; private set; }
        public string Currency { get; private set; } = "USD";

        public Address ShippingAddress { get; private set; } = null!;  // owned, frozen
        public string ContactPhone { get; private set; } = null!;

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items;

        public uint RowVersion { get; private set; }

        private Order() { }  // EF

        public Order(Guid userId,Guid idempotencyKey, Address shippingAddress, string contactPhone,
                     PaymentMethod paymentMethod, decimal shippingCost)
        {
            if (userId == Guid.Empty) throw new ArgumentException("Order must have an owner");
            if (string.IsNullOrWhiteSpace(contactPhone)) throw new ArgumentException("Contact phone is required");
            if (shippingCost < 0) throw new ArgumentException("Shipping cost cannot be negative");

            Id = Guid.NewGuid();
            IdempotencyKey = idempotencyKey;
            UserId = userId;
            ShippingAddress = shippingAddress;
            ContactPhone = contactPhone;
            PaymentMethod = paymentMethod;
            ShippingCost = shippingCost;

            Status = OrderStatus.Pending;
            PaymentStatus = PaymentStatus.Unpaid;
            Currency = "USD";
            CreatedAt = DateTime.UtcNow;
        }

        // ── building the order ──
        public void AddItem(OrderItem item)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Items can only be added to a pending order");

            _items.Add(item);
            RecalculateTotals();
        }

        public void AssignOrderNumber(string orderNumber)
        {
            //its check the property if its null then its assign the incoming order number
            //if not null then its cant be replaced
            if (!string.IsNullOrWhiteSpace(OrderNumber))
                throw new DomainException("Order number is already assigned");

            OrderNumber = orderNumber;
        }

        public void AssignPaymentSessionId(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(PaymentSessionId))
                throw new DomainException("Session id is already assigned");

            if (PaymentMethod == Entities.Enums.PaymentMethod.CashOnDelivery)
                throw new DomainException("Your payement is cash on delivery, you cant have session id");

            PaymentSessionId = sessionId;
        }

        //  status guards (only legal transitions) 
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

        //  payment guards 
        public void MarkPaid()
        {
            if(Status==OrderStatus.Cancelled)
                throw new DomainException("Cannot pay for a cancelled order");

            if (PaymentStatus != PaymentStatus.Unpaid)
                throw new DomainException("Order already paid or refunded");
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

        public void ReleaseReservation()
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Cannot release reservation on a non-pending order");

            if (PaymentStatus != PaymentStatus.Unpaid)
                throw new DomainException("Cannot release reservation on a paid order");

            if (!HasStockReservation)
                throw new DomainException("No reservation to release");

            HasStockReservation = false;
        }

        //  internal 
        private void RecalculateTotals()
        {
            Subtotal = _items.Sum(i => i.LineTotal);
            TotalAmount = Subtotal + ShippingCost;
        }

        private void Touch() => UpdatedAt = DateTime.UtcNow;
    }
}
