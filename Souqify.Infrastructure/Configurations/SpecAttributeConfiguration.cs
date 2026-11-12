

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Configurations
{
    public class SpecAttributeConfiguration : IEntityTypeConfiguration<SpecAttribute>
    {
        public void Configure(EntityTypeBuilder<SpecAttribute> builder)
        {
            builder.Property(sa => sa.Key).HasMaxLength(50);
            builder.Property(sa => sa.Name).HasMaxLength(50);
            builder.Property(sa => sa.DataType).HasConversion<string>().HasMaxLength(30);
            builder.Property(sa => sa.Unit).HasMaxLength(20);

            builder.HasIndex(sa => sa.Key).IsUnique();

            builder.HasMany(sa => sa.Options)
                .WithOne(so => so.SpecAttribute)
                .HasForeignKey(so => so.SpecAttributeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
