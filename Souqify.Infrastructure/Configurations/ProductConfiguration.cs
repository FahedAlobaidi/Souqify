using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Infrastructure.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            
            builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
            builder.Property(p => p.Brand).HasMaxLength(100).IsRequired();
            builder.Property(p=>p.BasePrice).HasPrecision(18, 2);
            builder.ToTable("Products", t => t.HasCheckConstraint("CK_BasePrice_MoreThanZero", "\"BasePrice\" > 0"));
            builder.Property(p => p.IsActive).HasDefaultValue(true);
            builder.Property(p => p.IsFeatured).HasDefaultValue(false);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(p => p.RowVersion).IsRowVersion();


            builder.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.ProductImages)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                

        }
    }
}
