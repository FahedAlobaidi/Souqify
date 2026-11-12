

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Configurations
{
    public class SpecOptionConfiguration : IEntityTypeConfiguration<SpecOption>
    {
        public void Configure(EntityTypeBuilder<SpecOption> builder)
        {
            builder.Property(so => so.Slug).HasMaxLength(50);
            builder.Property(so=>so.Value).HasMaxLength(50);

            builder.HasIndex(so => new { so.SpecAttributeId, so.Slug }).IsUnique();
        }
    }
}
