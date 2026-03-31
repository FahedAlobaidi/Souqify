

namespace Souqify.Application.DTOs.Admin
{
    public class AdminVariantDto
    {
        public Guid Id { get; set; }

        public string? Size { get; set; }

        public string? Color { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal PriceAdjustment { get; set; }

        public decimal FinalPrice { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; }

        public bool InStock { get; set; }

        public bool IsActive { get; set; }
    }
}
