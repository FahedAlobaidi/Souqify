using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Variant;

namespace Souqify.Application.DTOs.Product
{
    public class CreateProductDto
    {

        public required string Name { get; set; }

        public string? Description { get; set; }

        public decimal BasePrice { get; set; }

        public Guid CategoryId { get; set; }

        public required string Brand { get; set; }

        public bool IsFeatured { get; set; } = false;

        public required List<CreateVariantDto> Variants { get; set; }

        public required List<CreateImageDto> ProductImages { get; set; }
    }
}
