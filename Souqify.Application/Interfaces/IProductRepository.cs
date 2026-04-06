using Souqify.Application.Models;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductImage>> GetProductImagesAsync(Guid productId);
        Task<ProductVariant?> GetProductVariantByIdAsync(Guid productId, Guid productVariantId);
        Task<ProductImage?> GetProductImageByIdAsync(Guid productId, Guid productImageId);
        Task<Product?> GetProductByIdAsync(Guid productId);
        Task<decimal> GetProductBasePriceAsync(Guid productId);
        Task<ProductImage?> GetMainImageAsync(Guid productId);
        Task<Product> AddProductAsync(Product product);
        Task<ProductVariant> AddProductVariantAsync(Guid productId, ProductVariant productVariant);
        Task<ProductImage> AddProductImageAsync(Guid productId, ProductImage productImage);
        Task<bool> IsProductExistAsync(Guid id);
        Task<bool> IsSKUAlreadyExistsAsync(string sku, Guid currentVariantId);
        void DeactivateProduct(Product product);
        void DeleteProductImage(ProductImage productImage);
        Task<bool> SaveChangesAsync();

    }
}
