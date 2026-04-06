using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Image;
using Souqify.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Infrastructure.Queries
{
    public class AdminProductQueries : IAdminProductQueries
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public AdminProductQueries(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task<AdminProductDto?> GetAdminProductByIdAsync(Guid id)
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().Where(p => p.Id == id);

            return await collection.Select(p => new AdminProductDto
            {
                Id = p.Id,
                BasePrice = p.BasePrice,
                Brand = p.Brand,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Name = p.Name,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Description = p.Description,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                AllVariants = p.Variants.Select(v => new AdminVariantDto
                {
                    Id = v.Id,
                    Size = v.Size,
                    Color = v.Color,
                    IsActive = v.IsActive,
                    FinalPrice = p.BasePrice + v.PriceAdjustment,
                    PriceAdjustment = v.PriceAdjustment,
                    SKU = v.SKU,
                    InStock = v.StockQuantity > 0,
                    LowStockThreshold = v.LowStockThreshold,
                    StockQuantity = v.StockQuantity
                }).ToList(),
                Images = p.ProductImages.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    IsMain = img.IsMain,
                    DisplayOrder = img.DisplayOrder

                }).ToList(),
                TotalStock = p.Variants.Sum(v => v.StockQuantity)

            }).FirstOrDefaultAsync();
        }

        public async Task<AdminVariantDto?> GetAdminVariantByIdAsync(Guid productId, Guid variantId)
        {
            var collection = _souqifyDbContext.ProductVariants.AsNoTracking().Where(pv => pv.ProductId == productId && pv.Id == variantId);

            return await collection.Select(pv => new AdminVariantDto
            {
                Id = pv.Id,
                Color = pv.Color,
                Size = pv.Size,
                FinalPrice = pv.Product.BasePrice + pv.PriceAdjustment,
                InStock = pv.StockQuantity > 0,
                StockQuantity = pv.StockQuantity,
                PriceAdjustment = pv.PriceAdjustment,
                IsActive = pv.IsActive,
                LowStockThreshold = pv.LowStockThreshold,
                SKU = pv.SKU
            }).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<LowStockDto>> GetLowStockVariantsAsync()
        {
            var collection = _souqifyDbContext.ProductVariants.AsNoTracking().Where(v => v.IsActive && v.Product.IsActive && v.StockQuantity < v.LowStockThreshold);

            return await collection.OrderBy(v=>v.StockQuantity).Select(v => new LowStockDto
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                LowStockThreshold = v.LowStockThreshold,
                Size = v.Size,
                SKU = v.SKU,
                Color = v.Color,
                ProductName = v.Product.Name,
                StockQuantity = v.StockQuantity,
            }).ToListAsync();
        }
    }
}
