

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

            builder.OwnsOne(u => u.Address, a =>
            {
                a.Property(p => p.Street).HasColumnName("Street").HasMaxLength(200);
                a.Property(p => p.City).HasColumnName("City").HasMaxLength(100);
                a.Property(p => p.Region).HasColumnName("Region").HasMaxLength(100);
                a.Property(p => p.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
                a.Property(p => p.DeliveryNote).HasColumnName("DeliveryNote").HasMaxLength(300);
            });
        }
    }
}
