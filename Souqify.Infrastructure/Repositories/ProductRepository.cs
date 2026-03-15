using Microsoft.EntityFrameworkCore;
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

       

        public async Task<PagedList<Product>> GetAllProductsAsync(ProductQueryParams productQueryParams)
        {
            var collection = _souqifyDbContext.Products.AsNoTracking().AsQueryable();

            collection = collection.Where(p => p.IsActive);
            collection = ApplyFilter(productQueryParams, collection);

            var items = await collection.Skip((productQueryParams.CurrentPage - 1) * productQueryParams.PageSize).Take(productQueryParams.PageSize).ToListAsync();
            var totalItems = await collection.CountAsync();

            var pagedList = PagedList<Product>.CreatePagination(items, productQueryParams.PageSize, totalItems, productQueryParams.CurrentPage);

            return pagedList;
        }

        private IQueryable<Product> ApplyFilter(ProductQueryParams productQueryParams, IQueryable<Product> collection)
        {
            if (!string.IsNullOrWhiteSpace(productQueryParams.Brand))
            {
                productQueryParams.Brand = productQueryParams.Brand.Trim();
                collection = collection.Where(p => p.Brand == productQueryParams.Brand);
            }

            if (!string.IsNullOrWhiteSpace(productQueryParams.Category))
            {
                productQueryParams.Category = productQueryParams.Category.Trim();
                /*
                See what happened? EF Core saw you accessing p.Category.Name and automatically added the JOIN.
                You never loaded the Category object into memory — it just used it for filtering in SQL.
                */
                collection = collection.Where(p => p.Category.Name == productQueryParams.Category);
            }

            if (!string.IsNullOrWhiteSpace(productQueryParams.Sort))
            {
                productQueryParams.Sort = productQueryParams.Sort.Trim();
                collection = ApplySort(productQueryParams.Sort, collection);
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

        private IQueryable<Product> ApplySort(string sort,IQueryable<Product> collection)
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

        public async Task<IEnumerable<string>> GetBrandsAsync()
        {
            return await _souqifyDbContext.Products.AsNoTracking().Select(p => p.Brand).Distinct().ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductAsync()
        {
            return await _souqifyDbContext.Products.AsNoTracking().Where(p => p.IsFeatured).ToListAsync();
        }

        public async Task<IEnumerable<ProductVariant>> GetLowStockVariantsAsync()
        {
            return await _souqifyDbContext.ProductVariants.AsNoTracking().Where(pv => pv.StockQuantity < pv.LowStockThreshold).ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(Guid productId)
        {
            return await _souqifyDbContext.Products.AsNoTracking().Where(p=>p.Id==productId).Include(p=>p.Variants).Include(p=>p.ProductImages).Include(p=>p.Category).FirstOrDefaultAsync();
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

        private async Task<Product> GetProductOrThrowAsync(Guid productId)
        {
            var product = await _souqifyDbContext.Products
                .Include(p => p.Variants)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("No product with this id");

            return product;
        }
    }
}
