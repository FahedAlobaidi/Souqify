using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;


namespace Souqify.Infrastructure.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.Property(ci => ci.PriceAtAdded).HasColumnType("decimal(18,2)");
            builder.HasIndex(ci => new { ci.CartId, ci.ProductVariantId }).IsUnique();
            builder.Property(p => p.RowVersion).IsRowVersion();
            builder.ToTable("CartItem", t => t.HasCheckConstraint("CK_Quantity_MoreThanZero", "\"Quantity\" > 0"));
        }
    }

}
