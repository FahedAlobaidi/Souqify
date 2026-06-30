

using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.Models;
using Souqify.Domain;

namespace Souqify.Application.Interfaces
{
    public interface IProductQueries
    {
        Task<PagedList<ProductListDto>> GetAllProductsAsync(ProductQueryParams productQueryParams);
        Task<ProductDetailDto?> GetProductByIdAsync(Guid productId);
        Task<IEnumerable<CartItemsDto>> GetCartItemDetailsByVariantIdsAsync(List<Guid> variantIds);
        Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync();
        Task<IEnumerable<string>> GetBrandsAsync();
        Task<ProductImageDto?> GetProductImagesAsync(Guid productId, Guid imgId);
    }
}
