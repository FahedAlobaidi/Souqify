

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasIndex(ad => ad.UserId);
            builder.HasIndex(ad => ad.Timestamp);
            builder.Property(ad => ad.OldValues).HasColumnType("jsonb");
            builder.Property(ad => ad.NewValues).HasColumnType("jsonb");
            builder.Property(ad => ad.Action).IsRequired().HasMaxLength(50);
            builder.Property(ad => ad.EntityName).IsRequired().HasMaxLength(100);
        }
    }
}
