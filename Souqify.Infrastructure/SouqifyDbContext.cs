using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Souqify.Domain.Entities;
using Souqify.Infrastructure.Identity;


namespace Souqify.Infrastructure
{
    public class SouqifyDbContext: IdentityDbContext<ApplicationUser,IdentityRole<Guid>,Guid>
    {
        public DbSet<Product> Products { get; set; }

        public DbSet<ProductVariant> ProductVariants { get; set; }

        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public SouqifyDbContext(DbContextOptions<SouqifyDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SouqifyDbContext).Assembly);
        }
    }
}
