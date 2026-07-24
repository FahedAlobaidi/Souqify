

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;
using Souqify.Infrastructure.Identity;

namespace Souqify.Infrastructure.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasIndex(o => o.OrderNumber).IsUnique();
            builder.HasOne<ApplicationUser>()          
                .WithMany()                            
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(o => o.ContactPhone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(3);

            // Enums stored as readable text, not ints
            builder.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(o => o.PaymentStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(o => o.PaymentMethod)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Money
            builder.Property(o => o.Subtotal).HasPrecision(18, 2);
            builder.Property(o => o.ShippingCost).HasPrecision(18, 2);
            builder.Property(o => o.TotalAmount).HasPrecision(18, 2);

            // Address flattened into the Orders table
            builder.OwnsOne(o => o.ShippingAddress, a =>
            {
                a.Property(p => p.Street).IsRequired().HasColumnName("ShippingStreet").HasMaxLength(200);
                a.Property(p => p.City).IsRequired().HasColumnName("ShippingCity").HasMaxLength(100);
                a.Property(p => p.Region).IsRequired().HasColumnName("ShippingRegion").HasMaxLength(100);
                a.Property(p => p.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
                a.Property(p => p.DeliveryNote).HasColumnName("ShippingDeliveryNote").HasMaxLength(300);
            });

            builder.Property(o => o.RowVersion).IsRowVersion();

            builder.HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Load items INTO the private backing field
            builder.Metadata
                .FindNavigation(nameof(Order.Items))
                .SetPropertyAccessMode(PropertyAccessMode.Field);

        }
    }
}
