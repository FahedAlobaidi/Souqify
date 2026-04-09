using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Domain;


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
            productImage.ProductId = productId;//connect product image to product

            await _souqifyDbContext.ProductImages.AddAsync(productImage);

            return productImage;
        }

        public async Task<ProductVariant> AddProductVariantAsync(Guid productId, ProductVariant productVariant)
        {
            productVariant.ProductId = productId;

            await _souqifyDbContext.ProductVariants.AddAsync(productVariant);

            return productVariant;
        }

        public void DeactivateProduct(Product product)
        {
            product.IsActive = false;
        }

        public void DeleteProductImage(ProductImage productImage)
        {
             _souqifyDbContext.ProductImages.Remove(productImage);

        }

        public async Task<Product?> GetProductByIdAsync(Guid productId)
        {
            return await _souqifyDbContext.Products.Where(p=>p.Id==productId).Include(p=>p.Variants).Include(p=>p.ProductImages).Include(p=>p.Category).AsSplitQuery().FirstOrDefaultAsync();
        }

        public async Task<ProductImage?> GetProductImageByIdAsync(Guid productId, Guid productImageId)
        {
            return await _souqifyDbContext.ProductImages.FirstOrDefaultAsync(pi=>pi.Id==productImageId && pi.ProductId==productId);
        }

        public async Task<ProductVariant?> GetProductVariantByIdAsync(Guid productId, Guid productVariantId)
        {
            return await _souqifyDbContext.ProductVariants.FirstOrDefaultAsync(pv=>pv.Id==productVariantId && pv.ProductId==productId);

        }

        public async Task<IEnumerable<ProductImage>> GetProductImagesAsync(Guid productId)
        {
            return await _souqifyDbContext.ProductImages.Where(pi => pi.ProductId == productId).ToListAsync();
        }

        public async Task<bool> IsProductExistAsync(Guid id)
        {
            return await _souqifyDbContext.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsSKUAlreadyExistsAsync(string sku,Guid currentVariantId)
        {
            return await _souqifyDbContext.ProductVariants.AnyAsync(pv => pv.SKU == sku && pv.Id!= currentVariantId);
        }

        public async Task<decimal> GetProductBasePriceAsync(Guid productId)
        {
            //i used selcet to protect me bcs if the product null then the select return 0
            return await _souqifyDbContext.Products.Where(p => p.Id == productId).Select(p=>p.BasePrice).FirstOrDefaultAsync();
        }

        public async Task<ProductImage?> GetMainImageAsync(Guid productId)
        {
            return await _souqifyDbContext.ProductImages.Where(pi => pi.IsMain && pi.ProductId==productId).FirstOrDefaultAsync();
        }
    }
}
