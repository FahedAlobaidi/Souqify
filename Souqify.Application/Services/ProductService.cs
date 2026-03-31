using AutoMapper;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Category;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Services
{
    public class ProductService : IProductService
    {

        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository,ICategoryRepository categoryRepository,IMapper mapper)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public Task<ProductImageDto> AddImageAsync(Guid productId, CreateImageDto createImageDto)
        {
            throw new NotImplementedException();
        }

        public Task<AdminVariantDto> AddVariantAsync(Guid productId, CreateVariantDto createVariantDto)
        {
            throw new NotImplementedException();
        }

        public Task<AdminProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetBrandAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<CategoryDto>> GetCategoriesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<ProductListDto>> GetFeaturedProductsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LowStockDto>> GetLowStockVariantsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ProductDetailDto?> GetProductByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedList<ProductListDto>> GetProductsAsync(ProductQueryParams productQueryParams)
        {
            var pagedProducts = await _productRepository.GetAllProductsAsync(productQueryParams);

            var mappedItems = _mapper.Map<List<ProductListDto>>(pagedProducts.Items);

            var products = PagedList<ProductListDto>.CreatePagination(mappedItems, pagedProducts.PageSize, pagedProducts.TotalItems, pagedProducts.CurrentPage);

            return products;
        }

        public Task<bool> RemoveImageAsync(Guid productId, Guid ImageId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ToggleProductStatusAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<AdminProductDto> UpdateProductAsync(Guid productId, UpdateProductDto updateProductDto)
        {
            throw new NotImplementedException();
        }

        public Task<AdminVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateVariantDto updateVariantDto)
        {
            throw new NotImplementedException();
        }
    }
}
