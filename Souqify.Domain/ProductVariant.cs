using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Domain
{
    public class ProductVariant
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string? Size { get; set; } 

        public string? Color { get; set; }

        public byte[] RowVersion { get; set; } = [];

        public required string SKU { get; set; } = null!;//Stock Keeping Unit

        public decimal PriceAdjustment { get; set; } = 0;//+5.00 for XL, -2.00 for sale color

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 5; //alert when stock drops below this

        public bool IsActive { get; set; } = true;

        public Product? Product { get; set; }

        
    }
}
