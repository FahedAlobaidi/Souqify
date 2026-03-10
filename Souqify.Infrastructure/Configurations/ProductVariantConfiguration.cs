using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Infrastructure.Configurations
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.HasIndex(pv => pv.SKU).IsUnique();
            builder.Property(pv => pv.SKU).IsRequired();
            builder.Property(pv => pv.PriceAdjustment).HasPrecision(18, 2).HasDefaultValue(0);
            builder.ToTable("ProductVariants", t => t.HasCheckConstraint("CK_StockQuantity_moreOrEqualZero", "\"StockQuantity\" >= 0"));
            builder.Property(pv => pv.LowStockThreshold).HasDefaultValue(5);
            builder.Property(pv => pv.IsActive).HasDefaultValue(true);
            builder.Property(pv => pv.RowVersion).IsRowVersion();
        }
    }
}
