using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Product;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {

        private readonly SouqifyDbContext _souqifyDbContext;

        public ProductRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            await _souqifyDbContext.Products.AddAsync(product); 
            return product;
        }

        public async Task<ProductImage> AddProductImageAsync(Guid productId, ProductImage productImage)
        {
            var productEnt =await GetProductOrThrowAsync(productId);

            productImage.ProductId = productId;//connect product image to product

            await _souqifyDbContext.ProductImages.AddAsync(productImage);

            return productImage;
        }

        public async Task<ProductVariant> AddProductVariantAsync(Guid productId, ProductVariant productVariant)
        {
            var productEnt = await GetProductOrThrowAsync(productId);

            productVariant.ProductId = productId;

            await _souqifyDbContext.ProductVariants.AddAsync(productVariant);

            return productVariant;
        }

        public async Task DeactivateProductAsync(Guid productId)
        {
            var productEnt = await GetProductOrThrowAsync(productId);

            productEnt.IsActive = false;
        }

        public async Task DeleteProductImageAsync(Guid productId, Guid productImageId)
        {
            var productImgEnt = await GetProductImageByIdAsync(productId, productImageId);

            if (productImgEnt == null)
            {
                throw new Exception("No product image with this id");
            }

            _souqifyDbContext.ProductImages.Remove(productImgEnt);

        }

        public async Task<Product?> GetProductByIdAsync(Guid productId)
        {
            return await _souqifyDbContext.Products.Where(p=>p.Id==productId).Include(p=>p.Variants).Include(p=>p.ProductImages).Include(p=>p.Category).AsSplitQuery().FirstOrDefaultAsync();
        }

        public async Task<ProductImage?> GetProductImageByIdAsync(Guid productId, Guid productImageId)
        {
            await GetProductOrThrowAsync(productId);

            return await _souqifyDbContext.ProductImages.FindAsync(productImageId);
        }

        public async Task<ProductVariant?> GetProductVariantByIdAsync(Guid productId, Guid productVariantId)
        {
            await GetProductOrThrowAsync(productId);

            return await _souqifyDbContext.ProductVariants.FindAsync(productVariantId);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0;
        }

        //private async Task<Product> GetProductWithChildrenOrThrowAsync(Guid productId)
        //{
        //    var product = await _souqifyDbContext.Products
        //        .Include(p => p.Variants)
        //        .Include(p => p.ProductImages)
        //        .AsSplitQuery()
        //        .FirstOrDefaultAsync(p => p.Id == productId);

        //    if (product == null)
        //        throw new Exception("No product with this id");

        //    return product;
        //}

        private async Task<Product> GetProductOrThrowAsync(Guid productId)
        {
            var product = await _souqifyDbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("No product with this id");

            return product;
        }
    }
}
