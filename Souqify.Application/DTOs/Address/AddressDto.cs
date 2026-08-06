

namespace Souqify.Application.DTOs.Address
{
    public class AddressDto
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Region { get; set; } = null!;
        public string? PostalCode { get; set; }
        public string? DeliveryNote { get; set; }

    }
}
