using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Souqify.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Infrastructure.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasIndex(c => new { c.ParentId, c.Name }).IsUnique().AreNullsDistinct(false);
            builder.HasIndex(c => c.Slug).IsUnique();
            builder.Property(c => c.Slug).HasMaxLength(100);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.IsActive).HasDefaultValue(true);

            builder.HasOne(c=>c.Parent).WithMany(c=>c.Children).HasForeignKey(c=>c.ParentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
