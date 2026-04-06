

using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using Souqify.Domain;

namespace Souqify.Infrastructure.Queries
{
    public class ProductQueries : IProductQueries
    {

        private readonly SouqifyDbContext _souqifyDbContext;

        public ProductQueries(SouqifyDbContext context)
        {
            _souqifyDbContext = context;
        }

        public async Task<PagedList<ProductListDto>> GetAllProductsAsync(ProductQueryParams productQueryParams)
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().Where(p => p.IsActive);


            collection = ApplyFilter(productQueryParams, collection);
            var totalItems = await collection.CountAsync();

            var dtoCollection = collection.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                BasePrice = p.BasePrice,
                Brand = p.Brand,
                CategoryName = p.Category.Name,
                InStock = p.Variants.Any(v => v.StockQuantity > 0 && v.IsActive),
                IsFeatured = p.IsFeatured,
                MainImageUrl = p.ProductImages.Where(img => img.IsMain).Select(img => img.ImageUrl).FirstOrDefault() ?? string.Empty
            });

            var items = await dtoCollection.Skip((productQueryParams.CurrentPage - 1) * productQueryParams.PageSize).Take(productQueryParams.PageSize).ToListAsync();

            var pagedList = PagedList<ProductListDto>.CreatePagination(items, productQueryParams.PageSize, totalItems, productQueryParams.CurrentPage);

            return pagedList;
        }



        public async Task<IEnumerable<string>> GetBrandsAsync()
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().Where(p => p.IsActive);

            return await collection.Select(p => p.Brand).Distinct().ToListAsync();
        }

        public async Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync()
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().Where(p => p.IsFeatured && p.IsActive);

            return await collection.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                BasePrice = p.BasePrice,
                CategoryName = p.Category.Name,
                Brand = p.Brand,
                InStock = p.Variants.Any(v => v.StockQuantity > 0 && v.IsActive),
                IsFeatured = p.IsFeatured,
                MainImageUrl = p.ProductImages.Where(img => img.IsMain).Select(img => img.ImageUrl).FirstOrDefault() ?? string.Empty
            }).ToListAsync();
        }

        public async Task<ProductDetailDto?> GetProductByIdAsync(Guid productId)
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().Where(p => p.Id == productId && p.IsActive);

            var dtoCollection = collection.Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                BasePrice = p.BasePrice,
                IsFeatured = p.IsFeatured,
                Brand = p.Brand,
                CategoryName = p.Category.Name,
                Variants = p.Variants.Where(v => v.IsActive).Select(v => new VariantDto
                {
                    Id = v.Id,
                    Size = v.Size,
                    Color = v.Color,
                    SKU = v.SKU,
                    PriceAdjustment = v.PriceAdjustment,
                    FinalPrice = p.BasePrice + v.PriceAdjustment,
                    StockQuantity = v.StockQuantity,
                    InStock = v.StockQuantity > 0
                }).ToList(),
                Images = p.ProductImages.OrderBy(img=>img.DisplayOrder).Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    IsMain = img.IsMain,
                    DisplayOrder = img.DisplayOrder

                }).ToList()
            });

            var product = await dtoCollection.FirstOrDefaultAsync();


            return product;
        }

        public async Task<ProductImageDto?> GetProductImagesAsync(Guid productId, Guid imgId)
        {
            var collection = _souqifyDbContext.ProductImages.AsNoTracking().Where(pi => pi.ProductId == productId && pi.Id == imgId);

            return await collection.Select(pi => new ProductImageDto
            {
                Id = pi.Id,
                DisplayOrder = pi.DisplayOrder,
                ImageUrl = pi.ImageUrl,
                IsMain = pi.IsMain
            }).FirstOrDefaultAsync();
        }

        private IQueryable<Product> ApplyFilter(ProductQueryParams productQueryParams, IQueryable<Product> collection)
        {
            //have to make sort, if the sort is null or empty it will call default
            collection = ApplySort(productQueryParams.Sort?.Trim() ?? string.Empty, collection);

            if (!string.IsNullOrWhiteSpace(productQueryParams.Brand))
            {
                collection = collection.Where(p => p.Brand == productQueryParams.Brand.Trim());
            }

            if (!string.IsNullOrWhiteSpace(productQueryParams.Category))
            {

                /*
                See what happened? EF Core saw you accessing p.Category.Name and automatically added the JOIN.
                You never loaded the Category object into memory — it just used it for filtering in SQL.
                */
                collection = collection.Where(p => p.Category.Name == productQueryParams.Category.Trim());
            } 

            if (productQueryParams.MinPrice.HasValue)
            {
                collection = collection.Where(p => p.BasePrice >= productQueryParams.MinPrice);
            }

            if (productQueryParams.MaxPrice.HasValue)
            {
                collection = collection.Where(p => p.BasePrice <= productQueryParams.MaxPrice);
            }

            return collection;
        }

        private IQueryable<Product> ApplySort(string sort, IQueryable<Product> collection)
        {
            switch (sort)
            {
                case "priceAsc":
                    collection = collection.OrderBy(p => p.BasePrice);
                    break;
                case "priceDesc":
                    collection = collection.OrderByDescending(p => p.BasePrice);
                    break;
                case "nameAsc":
                    collection = collection.OrderBy(p => p.Name);
                    break;
                case "nameDesc":
                    collection = collection.OrderByDescending(p => p.Name);
                    break;
                default:
                    collection = collection.OrderBy(p => p.CreatedAt);
                    break;
            }

            return collection;
        }
    }
}
