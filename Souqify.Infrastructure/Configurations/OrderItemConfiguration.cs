
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasIndex(oi => oi.OrderId);

            // Frozen snapshots
            builder.Property(oi => oi.ProductNameSnapshot)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(oi => oi.VariantSnapshot).HasMaxLength(100);
            builder.Property(oi => oi.ProductImageSnapshot).HasMaxLength(500);

            builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Property(oi => oi.LineTotal).HasPrecision(18, 2);

            builder.Property(oi => oi.Quantity).IsRequired();

            builder.Property(oi => oi.RowVersion).IsRowVersion();
        }
    }
}
