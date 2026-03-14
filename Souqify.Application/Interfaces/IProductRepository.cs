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
        Task<IEnumerable<Product>> GetAllProductsAsync(ProductQueryParams productQueryParams);
        Task<Product?> GetProductByIdAsync(Guid productId);
        Task<IEnumerable<Product>> GetFeaturedProductAsync();
        Task DeactivateProductAsync(Guid productId);
        Task<IEnumerable<string>> GetBrandsAsync();
        Task<Product> AddProductAsync(Product product);
        Task<ProductVariant> AddProductVariantAsync(Guid productId, ProductVariant productVariant);
        Task<ProductVariant?> GetProductVariantByIdAsync(Guid productId, Guid productVariantId);
        Task<IEnumerable<ProductVariant>> GetLowStockVariantsAsync();
        Task<ProductImage> AddProductImageAsync(Guid productId, ProductImage productImage);
        Task<ProductImage?> GetProductImageByIdAsync(Guid productId, Guid productImageId);
        Task DeleteProductImageAsync(Guid productId, Guid productImageId);
        Task<bool> SaveChangesAsync();

    }
}
