
namespace Souqify.Application.DTOs.Product
{
    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public Guid CategoryId { get; set; }

        public bool IsFeatured { get; set; }

        public string Brand { get; set; } = string.Empty;
    }
}
