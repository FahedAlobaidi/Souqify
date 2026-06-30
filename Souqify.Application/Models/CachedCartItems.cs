

namespace Souqify.Application.Models
{
    public class CachedCartItems
    {
        public Guid Id { get; set; }

        public string ProductName { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public Guid ProductId { get; set; }

        //variant properties
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal PriceAtAdded { get; set; }//this is the price that labeled in the frontend when the user add the item
        public int Quantity { get; set; }//how many the user wants 
        public Guid VariantId { get; set; }

        //productImg property
        public string MainImgUrl { get; set; } = null!;
    }
}
