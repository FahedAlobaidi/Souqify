using AutoMapper;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Category;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using Souqify.Domain;
using System.Reflection.Metadata;


namespace Souqify.Application.Services
{
    public class ProductService : IProductService
    {

        private readonly IProductRepository _productRepository;
        private readonly ICategoryQueries _categoryQueries;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductQueries _productQueries;
        private readonly IAdminProductQueries _adminProductQueries;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository,ICategoryQueries categoryQueries,ICategoryRepository categoryRepository,IAdminProductQueries adminProductQueries,IMapper mapper,IProductQueries productQueries)
        {
            _productRepository = productRepository;
            _categoryQueries = categoryQueries;
            _categoryRepository = categoryRepository;
            _productQueries = productQueries;
            _adminProductQueries = adminProductQueries;
            _mapper = mapper;
        }

        public async Task<ProductImageDto> AddImageAsync(Guid productId, CreateImageDto createImageDto)
        {
            if(!await _productRepository.IsProductExistAsync(productId))
            {
                throw new KeyNotFoundException($"product with id {productId} not found");
            }

            if (createImageDto.IsMain)
            {
                var mainImg = await _productRepository.GetMainImageAsync(productId);

                if (mainImg != null)
                {
                    mainImg.IsMain = false; 
                }
            }

            var productImgEnt = _mapper.Map<ProductImage>(createImageDto);

            var productEnt = await _productRepository.AddProductImageAsync(productId, productImgEnt);

            await _productRepository.SaveChangesAsync();

            return await _productQueries.GetProductImagesAsync(productId, productImgEnt.Id) ?? throw new InvalidOperationException("Failed to retrieve image after creation");
        }

        public async Task<AdminVariantDto> AddVariantAsync(Guid productId, CreateVariantDto createVariantDto)
        {
            if (!await _productRepository.IsProductExistAsync(productId))
            {
                throw new KeyNotFoundException($"product with id {productId} not found");
            }

            if (await _productRepository.IsSKUAlreadyExistsAsync(createVariantDto.SKU, Guid.Empty))
            {
                throw new ArgumentException("SKU is not unique");
            }

            var basePrice = await _productRepository.GetProductBasePriceAsync(productId);

            if(basePrice+createVariantDto.PriceAdjustment <= 0)
            {
                throw new ArgumentException("final price must be more than 0");
            }

            var variantEnt = _mapper.Map<ProductVariant>(createVariantDto);

            variantEnt = await _productRepository.AddProductVariantAsync(productId, variantEnt);

            await _productRepository.SaveChangesAsync();

            return await _adminProductQueries.GetAdminVariantByIdAsync(productId, variantEnt.Id) ?? throw new Exception("Failed to retrieve variant after creation");
        }

        public async Task<AdminProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            //i added this ti check for duplicated SKU in the created productDto variants
            var hashedVariantsSku = new HashSet<string>();

            if (!await _categoryRepository.IsCategoryExistsAsync(createProductDto.CategoryId))
                throw new KeyNotFoundException("Wrong category id, there is no category with this id");

            foreach (var item in createProductDto.Variants)
            {
                if (!hashedVariantsSku.Add(item.SKU))
                    throw new ArgumentException("there are duplicated SKU");


                if(await _productRepository.IsSKUAlreadyExistsAsync(item.SKU,Guid.Empty))
                    throw new ArgumentException("SKU is not unique");

                if(createProductDto.BasePrice + item.PriceAdjustment <= 0)
                    throw new ArgumentException("final price must be more than 0");
            }

            var productEnt = _mapper.Map<Product>(createProductDto);

            productEnt.Id = Guid.NewGuid();

            await _productRepository.AddProductAsync(productEnt);

            await _productRepository.SaveChangesAsync();

            return await _adminProductQueries.GetAdminProductByIdAsync(productEnt.Id) ?? throw new InvalidOperationException("Failed to retrieve product after creation");
        }

        public async Task<IEnumerable<string>> GetBrandAsync()
        {
            return await _productQueries.GetBrandsAsync();
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            return await _categoryQueries.GetAllCategoriesAsync();
        }

        public async Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync()
        {
            return await _productQueries.GetFeaturedProductsAsync();
        }

        public async Task<IEnumerable<LowStockDto>> GetLowStockVariantsAsync()
        {
            return await _adminProductQueries.GetLowStockVariantsAsync();
        }

        public async Task<ProductDetailDto?> GetProductByIdAsync(Guid id)
        {
            var productDto = await _productQueries.GetProductByIdAsync(id);

            return productDto;
        }

        public async Task<PagedList<ProductListDto>> GetProductsAsync(ProductQueryParams productQueryParams)
        {
            var pagedProducts = await _productQueries.GetAllProductsAsync(productQueryParams);

            return pagedProducts;
        }

        public async Task<bool> RemoveImageAsync(Guid productId, Guid imageId)
        {
            var imageEnt = await _productRepository.GetProductImageByIdAsync(productId, imageId);

            if(imageEnt== null)
            {
                throw new KeyNotFoundException($"product with id {productId} or image id {imageId} not found");
            }

            if (!imageEnt.IsMain)
            {
                _productRepository.DeleteProductImage(imageEnt);

                await _productRepository.SaveChangesAsync();
            }
            else
            {
                var productImgs = await _productRepository.GetProductImagesAsync(productId);

                var numberOfImgs = productImgs.Count();

                if (numberOfImgs == 1)
                {
                    throw new InvalidOperationException("cant delete the image, there is only one image left");
                }

                //this to get the display order of the next image that after the main image 
                var nextProcductImg = productImgs.Where(pi =>pi.Id!=imageEnt.Id && pi.DisplayOrder > imageEnt.DisplayOrder ).FirstOrDefault();

                //if no image after the main then it will get the first image in the display order
                if (nextProcductImg == null)
                {
                    nextProcductImg = productImgs.First(pi => pi.Id != imageEnt.Id);
                }

                _productRepository.DeleteProductImage(imageEnt);

                nextProcductImg.IsMain = true;

                await _productRepository.SaveChangesAsync();
            }

            return true;

        }

        public async Task<bool> ToggleProductStatusAsync(Guid id)
        {
            var productEnt = await _productRepository.GetProductByIdAsync(id);

            if (productEnt == null)
            {
                throw new KeyNotFoundException($"Product with id {id} not found");
            }

            productEnt.IsActive = !productEnt.IsActive;

            await _productRepository.SaveChangesAsync();

            return productEnt.IsActive;
        }

        public async Task<AdminProductDto> UpdateProductAsync(Guid productId, UpdateProductDto updateProductDto)
        {
            var productEntUpdate =await _productRepository.GetProductByIdAsync(productId);

            if (productEntUpdate == null)
            {
                throw new KeyNotFoundException($"Product with id {productId} not found");
            }

            foreach(var item in productEntUpdate.Variants)
            {
                if(updateProductDto.BasePrice + item.PriceAdjustment <=0)
                    throw new ArgumentException("final price must be more than 0");
            }

            _mapper.Map(updateProductDto, productEntUpdate);

            productEntUpdate.UpdatedAt = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync();

            return await _adminProductQueries.GetAdminProductByIdAsync(productId) ?? throw new InvalidOperationException("Failed to retrieve product after update");
        }

        public async Task<AdminVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateVariantDto updateVariantDto)
        {
            var variantEnt =await _productRepository.GetProductVariantByIdAsync(productId, variantId);

            if (variantEnt == null)
            {
                throw new KeyNotFoundException($"Product with id {productId} or variant with id {variantId} not found");
            }

            if (await _productRepository.IsSKUAlreadyExistsAsync(updateVariantDto.SKU, variantId))
            {
                throw new ArgumentException("SKU is not unique");
            }

            var basePrice = await _productRepository.GetProductBasePriceAsync(productId);

            var finalPrice = basePrice + updateVariantDto.PriceAdjustment;

            if(finalPrice <= 0)
            {
                throw new ArgumentException("final price must be more than 0");
            }

            _mapper.Map(updateVariantDto,variantEnt);

            await _productRepository.SaveChangesAsync();

             
            return await _adminProductQueries.GetAdminVariantByIdAsync(productId, variantId) ?? throw new InvalidOperationException("Failed to retrieve product after update");
        }
    }
}
