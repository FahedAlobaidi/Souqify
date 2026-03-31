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
        Task DeactivateProductAsync(Guid productId);
        Task<Product> AddProductAsync(Product product);
        Task<Product?> GetProductByIdAsync(Guid productId);
        Task<ProductVariant> AddProductVariantAsync(Guid productId, ProductVariant productVariant);
        Task<ProductImage> AddProductImageAsync(Guid productId, ProductImage productImage);
        Task DeleteProductImageAsync(Guid productId, Guid productImageId);
        Task<ProductVariant?> GetProductVariantByIdAsync(Guid productId, Guid productVariantId);
        Task<ProductImage?> GetProductImageByIdAsync(Guid productId, Guid productImageId);
        Task<bool> SaveChangesAsync();

    }
}
