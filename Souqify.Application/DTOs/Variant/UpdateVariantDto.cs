
namespace Souqify.Application.DTOs.Variant
{
    public class UpdateVariantDto
    {
        public string? VariantName { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal PriceAdjustment { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; }

        public bool IsActive { get; set; }
    }
}
