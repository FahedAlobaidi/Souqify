using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
using Souqify.Infrastructure;
using Souqify.Infrastructure.Identity;
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

        /// <summary>
        /// Same as SeedProductAsync but with a known stock and price, so order tests can
        /// assert on exact numbers instead of whatever Bogus picked.
        /// </summary>
        public static async Task<Product> SeedProductWithStockAsync(
            IServiceProvider serviceProvider,
            int stockQuantity,
            decimal basePrice = 20m,
            decimal priceAdjustment = 5m)
        {
            using var scope = serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var faker = new Faker();

            var category = await SeedCategoryAsync(serviceProvider);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = faker.Commerce.ProductName(),
                Brand = faker.Company.CompanyName(),
                Description = faker.Commerce.ProductDescription(),
                BasePrice = basePrice,
                CategoryId = category.Id,
                IsActive = true,
                IsFeatured = false,
                CreatedAt = DateTime.UtcNow
            };

            product.Variants = new List<ProductVariant>
            {
                new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    SKU = faker.Random.AlphaNumeric(10).ToUpper(),
                    Color = "Black",
                    Size = "L",
                    LowStockThreshold = 5,
                    PriceAdjustment = priceAdjustment,
                    StockQuantity = stockQuantity,
                    IsActive = true
                }
            };

            product.ProductImages = new List<ProductImage>
            {
                new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ImageUrl = faker.Image.PicsumUrl(),
                    DisplayOrder = 1,
                    IsMain = true
                }
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product;
        }

        /// <summary>
        /// Puts one line straight into the user's cart, skipping the cart endpoints.
        /// Order tests care about what checkout does with a cart, not about how it got there.
        /// </summary>
        public static async Task<Cart> SeedCartAsync(
            IServiceProvider serviceProvider,
            Guid userId,
            Guid productId,
            Guid variantId,
            int quantity,
            decimal priceAtAdded)
        {
            using var scope = serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var cart = new Cart(userId);
            cart.AddItem(new CartItem(productId, variantId, cart.Id, quantity, priceAtAdded));

            db.Carts.Add(cart);
            await db.SaveChangesAsync();

            return cart;
        }

        /// <summary>
        /// Creates a user and issues a real JWT for them, without calling /api/auth/register.
        /// That endpoint is rate limited to 5 requests per minute per IP ("LoginLimiter" in
        /// Program.cs) and one test class needs far more than that. The token comes from the
        /// app's own IJwtTokenService, so it is the same token the auth endpoints hand out.
        /// </summary>
        public static async Task<(string accessToken, Guid userId)> RegisterUserAsync(
            IServiceProvider serviceProvider)
        {
            var faker = new Faker();
            var email = $"{Guid.NewGuid():N}@souqify-test.com";

            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                FirstName = faker.Person.FirstName,
                LastName = faker.Person.LastName,
                PhoneNumber = "0791234567",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, "Sa123456");

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Test user could not be created: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));

            var roles = (await userManager.GetRolesAsync(user)).ToList();

            var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            return (jwtTokenService.GenerateAccessToken(user.Id, user.Email!, roles), user.Id);
        }
    }
}
