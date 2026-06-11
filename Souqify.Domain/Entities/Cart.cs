

namespace Souqify.Domain.Entities
{
    public class Cart
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; private set; }

        public Guid? GuestId { get; private set; }

        public DateTime ExpireAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public List<CartItem> CartItems { get;private set; } = new List<CartItem>();

        public decimal TotalPrice => CartItems.Sum(ci => ci.PriceAtAdd * ci.Quantity);

        private Cart() { }  // EF uses this

        public Cart(Guid? userId,Guid? guestId)
        {
            if(userId is null && guestId is null)// this to be sure that only one of userId and guestId has data
            {
                throw new ArgumentException("Cart must have an owner");
            }

            if(userId is not null && guestId is not null)//to insure that guestId and userId one of them not null
            {
                throw new ArgumentException("Cart must have only one owner");
            }

            CreatedAt = DateTime.UtcNow;

            if (userId is not null)
            {
                ExpireAt = DateTime.UtcNow.AddDays(7);
                
            }else if(guestId is not null)
            {
                ExpireAt = DateTime.UtcNow.AddDays(3);
            }

            UserId = userId;
            GuestId = guestId;
        }
    }
}
