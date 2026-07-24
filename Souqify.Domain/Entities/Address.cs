

namespace Souqify.Domain.Entities
{
    public class Address
    {
        public string Street { get; private set; } = null!;
        public string City { get; private set; } = null!;
        public string Region { get; private set; } = null!;
        public string? PostalCode { get; private set; }
        public string? DeliveryNote { get; private set; }

        private Address() { }  // EF

        public Address(string street, string city, string region, string? postalCode = null, string? deliveryNote = null)
        {
            if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required");
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required");
            if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Region is required");

            Street = street;
            City = city;
            Region = region;
            PostalCode = postalCode;
            DeliveryNote = deliveryNote;
        }
    }
}
