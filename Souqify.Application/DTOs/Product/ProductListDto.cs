namespace Souqify.Application.DTOs.Product
{
    public class ProductListDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public string Brand { get; set; } = string.Empty;

        //i will get this from Category table(entity)
        public string CategoryName { get; set; } = string.Empty;

        //true if ANY active variant has StockQuantity > 0
        public bool InStock { get; set; }

        public bool IsFeatured { get; set; }

        //computed from Images where IsMain = true
        public string MainImageUrl { get; set; } = string.Empty;
    }
}
