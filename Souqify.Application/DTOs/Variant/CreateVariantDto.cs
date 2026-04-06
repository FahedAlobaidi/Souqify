using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.DTOs.Variant
{
    public class CreateVariantDto
    {
        public string? Size { get; set; }

        public string? Color { get; set; }

        public required string SKU { get; set; }

        public decimal PriceAdjustment { get; set; } = 0;

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 5;
    }
}
