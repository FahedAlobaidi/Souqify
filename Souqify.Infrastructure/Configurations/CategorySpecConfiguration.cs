

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Configurations
{
    public class CategorySpecConfiguration : IEntityTypeConfiguration<CategorySpec>
    {
        public void Configure(EntityTypeBuilder<CategorySpec> builder)
        {
            builder.HasKey(cs => new { cs.CategoryId, cs.SpecAttributeId });

            builder.HasOne(cs => cs.Category)
                .WithMany(c => c.Specs)
                .HasForeignKey(cs => cs.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cs => cs.SpecAttribute)
                .WithMany()
                .HasForeignKey(cs => cs.SpecAttributeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
