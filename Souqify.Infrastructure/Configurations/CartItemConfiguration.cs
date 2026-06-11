using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;


namespace Souqify.Infrastructure.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {

            builder.Property(ci => ci.PriceAtAdd).HasColumnType("decimal(18,2)");
            builder.HasIndex(ci => new { ci.CartId, ci.ProductId, ci.ProductVariantId }).IsUnique();
            builder.ToTable("CartItem", t => t.HasCheckConstraint("CK_Quantity_MoreThanZero", "\"Quantity\" > 0"));
        }
    }

}
