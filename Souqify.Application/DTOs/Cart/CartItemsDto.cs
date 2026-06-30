

namespace Souqify.Application.DTOs.Cart
{
    public class CartItemsDto
    {
        public Guid Id { get; set; }

        //product properties
        public string ProductName { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public Guid ProductId { get; set; }

        //variant properties
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal CurrentPrice { get; set; }// this price i get it from DB to see if it different from the price at added 
        public decimal PriceAtAdded { get; set; }//this is the price that labeled in the frontend when the user add the item
        public bool PriceChanged { get; set; }//this to check if (CurrentPrice and PriceAtAdded) changed or not
        public int Quantity { get; set; }//how many the user wants 
        public int AvailableStock { get; set; }   // how many the warehouse can actually sell right now
        public bool InStock { get; set; }
        public bool ExceedsStock { get; set; } // to check if the variant quantity is more than what user needs
        public decimal lineTotal { get; set; }//how much the price that the cart item line has (current  * quantity)
        public Guid VariantId { get; set; }

        //productImg property
        public string MainImgUrl { get; set; } = null!;
    }
}
