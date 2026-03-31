namespace Souqify.Application.DTOs.Variant
{
    public class VariantDto
    {
        public Guid Id { get; set; }

        public string? Size { get; set; }

        public string? Color { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal PriceAdjustment { get; set; }

        public decimal FinalPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool InStock { get; set; }
    }
}
