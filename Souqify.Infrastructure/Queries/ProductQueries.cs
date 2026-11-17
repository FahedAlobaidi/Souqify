
using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;
using System.Globalization;

namespace Souqify.Infrastructure.Queries
{
    public class ProductQueries : IProductQueries
    {

        private readonly SouqifyDbContext _souqifyDbContext;

        public ProductQueries(SouqifyDbContext context)
        {
            _souqifyDbContext = context;
        }

        public async Task<PagedList<ProductListDto>> GetAllProductsAsync(ProductQueryParams productQueryParams,List<Guid>? categoryIds)
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().Where(p => p.IsActive);


            collection = ApplyFilter(productQueryParams, categoryIds ,collection);
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

        public async Task<IEnumerable<CartItemDto>> GetCartItemDetailsByVariantIdsAsync(List<Guid> variantIds)
        {
            var collection = _souqifyDbContext.ProductVariants.AsNoTracking().Where(pv => variantIds.Contains(pv.Id) && pv.IsActive && pv.Product.IsActive);


            //this dto filled by two sides cache and DB
            //so every property not here it will filled in cache service from redis
            var cartItemDto = await collection.Select(pv => new CartItemDto
            {
                ProductId = pv.ProductId,
                ProductName = pv.Product.Name,
                Brand = pv.Product.Brand,

                VariantId = pv.Id,
                VariantName=pv.VariantName,
                CurrentPrice = pv.PriceAdjustment + pv.Product.BasePrice,
                InStock = pv.StockQuantity > 0,
                AvailableStock = pv.StockQuantity,

                
                MainImgUrl = pv.Product.ProductImages.Where(pi=>pi.IsMain).Select(pi => pi.ImageUrl).FirstOrDefault()
            }).ToListAsync();

            return cartItemDto;
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

        public Task<decimal?> GetFinalPrice(Guid variantId)
        {
            return _souqifyDbContext.ProductVariants.AsNoTracking().Where(pv=>pv.Id==variantId && pv.IsActive && pv.Product.IsActive).Select(pv=>(decimal?)(pv.PriceAdjustment+pv.Product.BasePrice)).FirstOrDefaultAsync();
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
                    VariantName = v.VariantName,
                    SKU = v.SKU,
                    PriceAdjustment = v.PriceAdjustment,
                    FinalPrice = p.BasePrice + v.PriceAdjustment,
                    StockQuantity = v.StockQuantity,
                    InStock = v.StockQuantity > 0
                }).ToList(),
                ProductImages = p.ProductImages.OrderBy(img=>img.DisplayOrder).Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    IsMain = img.IsMain,
                    DisplayOrder = img.DisplayOrder

                }).ToList()
            });

            var product = await dtoCollection.FirstOrDefaultAsync();

            if (product == null)
                return null;

            // CAT-4 AC 5: the product's answers, one row each, with what the page needs to show them.
            var specRows = await _souqifyDbContext.ProductSpecValues.AsNoTracking()
                .Where(sv => sv.ProductId == productId)
                .Select(sv => new
                {
                    Key = sv.SpecAttribute!.Key,
                    Name = sv.SpecAttribute.Name,
                    Unit = sv.SpecAttribute.Unit,
                    DataType = sv.SpecAttribute.DataType,
                    OptionValue = sv.OptionId != null ? sv.Option!.Value : null,
                    OptionOrder = sv.OptionId != null ? sv.Option!.DisplayOrder : 0,
                    sv.NumberValue,
                    sv.BoolValue,
                    sv.TextValue,
                    // DisplayOrder lives on the form row: this product's category and this question.
                    FormOrder = _souqifyDbContext.CategorySpecs
                        .Where(cs => cs.CategoryId == sv.Product!.CategoryId && cs.SpecAttributeId == sv.SpecAttributeId)
                        .Select(cs => (int?)cs.DisplayOrder)
                        .FirstOrDefault()
                })
                .OrderBy(r => r.FormOrder)
                .ThenBy(r => r.Key)
                .ThenBy(r => r.OptionOrder)
                .ToListAsync();

            // Many rows in, one line per question out: a MultiOption answer has one row per choice, but the customer sees one line.
            foreach (var row in specRows)
            {
                // Find-or-create this question's line, the same as the MultiOption block in SpecQueries.GetProductSpecsAsync.
                ProductSpecDto? spec = product.Specs.FirstOrDefault(s => s.Key == row.Key);

                if (spec == null)
                {
                    spec = new ProductSpecDto();
                    spec.Key = row.Key;
                    spec.Name = row.Name;
                    spec.Unit = row.Unit;
                    product.Specs.Add(spec);
                }

                // The value as the customer reads it.
                switch (row.DataType)
                {
                    case SpecDataType.Option:
                    case SpecDataType.MultiOption:
                        spec.Values.Add(row.OptionValue!);      // "RTX 5070", not the slug
                        break;
                    case SpecDataType.Number:
                        spec.Values.Add(row.NumberValue!.Value.ToString("0.###", CultureInfo.InvariantCulture));   // 12.000 becomes "12", 5.400 becomes "5.4"
                        break;
                    case SpecDataType.Boolean:
                        spec.Values.Add(row.BoolValue == true ? "Yes" : "No");
                        break;
                    case SpecDataType.Text:
                        spec.Values.Add(row.TextValue!);
                        break;
                }
            }

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

        private IQueryable<Product> ApplyFilter(ProductQueryParams productQueryParams, List<Guid>? categoryIds, IQueryable<Product> collection)
        {
            //have to make sort, if the sort is null or empty it will call default
            collection = ApplySort(productQueryParams.Sort?.Trim() ?? string.Empty, collection);

            if (!string.IsNullOrWhiteSpace(productQueryParams.Brand))
            {
                collection = collection.Where(p => p.Brand == productQueryParams.Brand.Trim());
            }

            if (!(categoryIds == null || categoryIds.Count == 0))
            {
                collection = collection.Where(p => categoryIds.Contains(p.CategoryId));
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
