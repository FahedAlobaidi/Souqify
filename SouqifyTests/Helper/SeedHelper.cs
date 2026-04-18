using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Souqify.Domain;
using Souqify.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SouqifyTests.Helper
{
    public static class SeedHelper
    {
        public static async Task<Category> SeedCategoryAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var faker = new Faker();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = faker.Commerce.Categories(1)[0] + Guid.NewGuid().ToString()[..6],
                IsActive=true,
                Description=faker.Commerce.ProductDescription()
            };


            db.Categories.Add(category);
           

            await db.SaveChangesAsync();

            return category;
        }

        public static async Task<Product> SeedProductAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var faker = new Faker();

            var category = await SeedCategoryAsync(serviceProvider);

            var product = new Product
            {
                Brand = faker.Company.CompanyName(),
                Description = faker.Commerce.ProductDescription(),
                Name=faker.Commerce.ProductName(),
                BasePrice= decimal.Parse(faker.Commerce.Price(1, 100)),
                CategoryId=category.Id,
                IsActive=true,
                Id=Guid.NewGuid(),
                CreatedAt=DateTime.UtcNow,
                IsFeatured=true,
                
            };

            product.Variants = new List<ProductVariant>
            {
                    new ProductVariant
                    {
                        SKU=faker.Random.AlphaNumeric(10).ToUpper(),
                        Color=faker.Commerce.Color(),
                        LowStockThreshold=5,
                        PriceAdjustment=faker.Random.Int(1,10),
                        Size=faker.PickRandom("S","M","L","XL"),
                        StockQuantity=faker.Random.Int(1,100),
                        Id=Guid.NewGuid(),
                        IsActive=true,
                        ProductId=product.Id
                    }
            };

            product.ProductImages = new List<ProductImage>
            {
                new ProductImage
                {
                    Id=Guid.NewGuid(),
                    ProductId=product.Id,
                    ImageUrl=faker.Image.PicsumUrl(),
                    DisplayOrder=1,
                    IsMain=true
                }
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product;
        }
    }
}
