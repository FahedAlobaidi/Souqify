

using System.Formats.Asn1;

namespace Souqify.Domain.Entities
{
    public class Cart
    {
        public Guid Id { get;private set; }

        public Guid UserId { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime LastModifiedAt { get; private set; }

        private readonly List<CartItem> _CartItems = new List<CartItem>();

        public IReadOnlyList<CartItem> CartItems => _CartItems;

        public uint RowVersion { get;private set; }

        public decimal TotalPrice => _CartItems.Sum(ci => ci.PriceAtAdded * ci.Quantity);

        private Cart() { }  // EF uses this

        public Cart(Guid userId)
        {
            if(userId == Guid.Empty)// a logged-in cart cannot exist without a userId
            {
                throw new ArgumentException("Cart must have an owner");
            }

            CreatedAt = DateTime.UtcNow;
            LastModifiedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddDays(30);

            UserId = userId;
            Id=Guid.NewGuid();  
        }

        public void AddItem(CartItem cartItem) 
        {
            var item = _CartItems.Where(ci => ci.ProductVariantId == cartItem.ProductVariantId).FirstOrDefault();

            if (item == null)
            {
                _CartItems.Add(cartItem);
            }
            else
            {
                item.IncreaseQuantity(cartItem.Quantity);
            }


            Touch();
        }

        public void RemoveItem(CartItem item)
        {
            _CartItems.Remove(item);
            Touch();
        }

        private void Touch()
        {
            LastModifiedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddDays(30);
        }
    }
}
