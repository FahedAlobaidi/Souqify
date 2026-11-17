

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Configurations
{
    public class ProductSpecValueConfiguration : IEntityTypeConfiguration<ProductSpecValue>
    {
        public void Configure(EntityTypeBuilder<ProductSpecValue> builder)
        {
            builder.Property(psv => psv.NumberValue).HasPrecision(12, 3);
            builder.Property(psv => psv.TextValue).HasMaxLength(200);

            // Exactly one of OptionId, NumberValue, BoolValue, TextValue is filled. The other 3 must be null.
            builder.ToTable("ProductSpecValues", t => t.HasCheckConstraint("CK_ProductSpecValues_ExactlyOneValue",
                "num_nonnulls(\"OptionId\", \"NumberValue\", \"BoolValue\", \"TextValue\") = 1"));

            // One row per answer. A MultiOption answer has one row per choice, so OptionId is part of the index.
            // OptionId is null for Number, Boolean and Text answers. AreNullsDistinct(false) makes those nulls equal, so a product can't get two vram_gb rows.
            builder.HasIndex(psv => new { psv.ProductId, psv.SpecAttributeId, psv.OptionId }).IsUnique().AreNullsDistinct(false);

            builder.HasOne(psv => psv.Product)
                .WithMany(p => p.SpecValues)
                .HasForeignKey(psv => psv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(psv => psv.SpecAttribute)
                .WithMany()
                .HasForeignKey(psv => psv.SpecAttributeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(psv => psv.Option)
                .WithMany()
                .HasForeignKey(psv => psv.OptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
