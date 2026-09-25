

using Souqify.Application.DTOs.Address;
using Souqify.Domain.Entities.Enums;

namespace Souqify.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        public AddressDto ShippingAddress { get; set; } = null!;
        public string ContactPhone { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; }
    }
}
