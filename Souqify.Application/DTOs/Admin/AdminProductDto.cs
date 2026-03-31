using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Variant;


namespace Souqify.Application.DTOs.Admin
{
    public class AdminProductDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Brand { get; set; } = string.Empty;

        public int TotalStock { get; set; }

        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();

        public List<AdminVariantDto> AllVariants { get; set; } = new List<AdminVariantDto>();
    }
}
