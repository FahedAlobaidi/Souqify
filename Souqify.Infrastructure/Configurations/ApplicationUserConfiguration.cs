

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Infrastructure.Identity;

namespace Souqify.Infrastructure.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(au => au.FirstName).IsRequired().HasMaxLength(50);
            builder.Property(au => au.LastName).IsRequired().HasMaxLength(50);
            builder.Property(au => au.CreatedAt).IsRequired();
        }
    }
}
