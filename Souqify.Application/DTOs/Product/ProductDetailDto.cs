using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Variant;

namespace Souqify.Application.DTOs.Product
{
    public class ProductDetailDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        //from Category table
        public string CategoryName { get; set; } = string.Empty;

        public bool IsFeatured { get; set; }

        public string Brand { get; set; } = string.Empty;

        public List<ProductImageDto> ProductImages { get; set; } = new List<ProductImageDto>();

        public List<VariantDto> Variants { get; set; } = new List<VariantDto>();
    }
}
