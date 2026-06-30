

using System.Formats.Asn1;

namespace Souqify.Domain.Entities
{
    public class Cart
    {
        public Guid Id { get; set; }

        public Guid UserId { get; private set; }

        public DateTime LastModifiedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public List<CartItem> CartItems { get;private set; } = new List<CartItem>();

        public uint RowVersion { get;private set; }

        public decimal TotalPrice => CartItems.Sum(ci => ci.PriceAtAdd * ci.Quantity);

        private Cart() { }  // EF uses this

        public Cart(Guid userId)
        {
            if(userId == Guid.Empty)// this to be sure that only one of userId and guestId has data
            {
                throw new ArgumentException("Cart must have an owner");
            }

            CreatedAt = DateTime.UtcNow;
            LastModifiedAt = DateTime.UtcNow.AddDays(30);

            UserId = userId;
        }
    }
}
