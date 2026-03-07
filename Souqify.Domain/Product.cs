using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Domain
{
    public class Product
    {
        public Guid Id { get; set; }

        public required string Name { get; set; } = null!;

        public required string Description { get; set; } = null!;

        public decimal BasePrice { get; set; }

        public Guid CategoryId { get; set; }

        public required string Brand { get; set; } = null!;

        public bool IsActive { get; set; } = true;//admin can hide products without deleting

        public bool IsFeatured { get; set; } = false;//gomepage featured products

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

        public byte[] RowVersion { get; set; } = [];//concurrency token

        public Category? Category { get; set; }

        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        public List<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
