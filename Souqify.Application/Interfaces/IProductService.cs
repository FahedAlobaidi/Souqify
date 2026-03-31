using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Category;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Models;

namespace Souqify.Application.Interfaces
{
    public interface IProductService
    {
        public Task<PagedList<ProductListDto>> GetProductsAsync(ProductQueryParams productQueryParams);

        public Task<ProductDetailDto?> GetProductByIdAsync(Guid id);

        public Task<List<ProductListDto>> GetFeaturedProductsAsync();

        public Task<List<string>> GetBrandAsync();

        public Task<List<CategoryDto>> GetCategoriesAsync();

        public Task<AdminProductDto> CreateProductAsync(CreateProductDto createProductDto);

        public Task<AdminProductDto> UpdateProductAsync(Guid productId,UpdateProductDto updateProductDto);

        public Task<bool> ToggleProductStatusAsync(Guid id);

        public Task<AdminVariantDto> AddVariantAsync(Guid productId, CreateVariantDto createVariantDto);

        public Task<AdminVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateVariantDto updateVariantDto);

        public Task<ProductImageDto> AddImageAsync(Guid productId, CreateImageDto createImageDto);

        public Task<bool> RemoveImageAsync(Guid productId, Guid ImageId);

        public Task<IEnumerable<LowStockDto>> GetLowStockVariantsAsync();
    }
}
