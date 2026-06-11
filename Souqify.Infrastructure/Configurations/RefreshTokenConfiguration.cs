
using Souqify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Infrastructure.Identity;

namespace Souqify.Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Token).IsRequired().HasMaxLength(200);
            builder.HasIndex(r => r.Token).IsUnique();
            builder.Property(r => r.ExpiresAt).IsRequired();
            builder.Property(r => r.CreatedAt).IsRequired();

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r=>r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
