using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.DTOs.Admin
{
    public class LowStockDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public Guid VariantId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string? Size { get; set; }

        public string? Color { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; }
    }
}
