
namespace Souqify.Application.DTOs.Variant
{
    public class UpdateVariantDto
    {
        public string? Size { get; set; }

        public string? Color { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal PriceAdjustment { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; }

        public bool IsActive { get; set; }
    }
}
