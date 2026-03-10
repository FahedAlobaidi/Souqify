using Microsoft.EntityFrameworkCore;
using Souqify.Domain;


namespace Souqify.Infrastructure
{
    public class SouqifyDbContext: DbContext
    {
        public DbSet<Product> Products { get; set; }

        public DbSet<ProductVariant> ProductVariants { get; set; }

        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Category> Categories { get; set; }

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
