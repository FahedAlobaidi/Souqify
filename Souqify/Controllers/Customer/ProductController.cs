using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Souqify.Application.DTOs.Category;
using Souqify.Application.DTOs.Product;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using Souqify.Helpers;
using System.Text.Json;

namespace Souqify.Controllers.Customer
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet(Name = "GetAllProducts")]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetAllProducts([FromQuery] ProductQueryParams productQueryParams)
        {
            var products = await _productService.GetProductsAsync(productQueryParams);

            var previousPageLink = products.HasPrevious ? CreateProductUri(productQueryParams, PaginationLinkType.PreviousPage) : null;

            var nextPageLink = products.HasNext ? CreateProductUri(productQueryParams, PaginationLinkType.NextPage) : null;

            var paginationMetaData = new
            {
                currentPage = products.CurrentPage,
                totalPages = products.TotalPages,
                pageSize = products.PageSize,
                totalItems = products.TotalItems,
                previousPage = previousPageLink,
                nextPage = nextPageLink
            };

            Response.Headers["X-Pagination"] = JsonSerializer.Serialize(paginationMetaData);

            return Ok(products.Items);
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<ProductDetailDto>> GetProductById(Guid productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetFeaturedProducts()
        {
            var featuredProducts = await _productService.GetFeaturedProductsAsync();

            return Ok(featuredProducts);
        }

        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<string>>> GetBrands()
        {
            return Ok(await _productService.GetBrandAsync());
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            return Ok(await _productService.GetCategoriesAsync());
        }

        private string? CreateProductUri(ProductQueryParams productQueryParams, PaginationLinkType type)
        {
            switch (type)
            {
                case PaginationLinkType.PreviousPage:
                    return Url.Link("GetAllProducts", new
                    {
                        sort = productQueryParams.Sort,
                        currentPage = productQueryParams.CurrentPage - 1,
                        pageSize = productQueryParams.PageSize,
                        minPrice = productQueryParams.MinPrice,
                        maxPrice = productQueryParams.MaxPrice,
                        category = productQueryParams.Category,
                        brand = productQueryParams.Brand
                    });

                case PaginationLinkType.NextPage:
                    return Url.Link("GetAllProducts", new
                    {
                        sort = productQueryParams.Sort,
                        currentPage = productQueryParams.CurrentPage + 1,
                        pageSize = productQueryParams.PageSize,
                        minPrice = productQueryParams.MinPrice,
                        maxPrice = productQueryParams.MaxPrice,
                        category = productQueryParams.Category,
                        brand = productQueryParams.Brand
                    });

                default:
                    return Url.Link("GetAllProducts", new
                    {
                        sort = productQueryParams.Sort,
                        currentPage = productQueryParams.CurrentPage,
                        pageSize = productQueryParams.PageSize,
                        minPrice = productQueryParams.MinPrice,
                        maxPrice = productQueryParams.MaxPrice,
                        category = productQueryParams.Category,
                        brand = productQueryParams.Brand
                    });
            }
        }
    }
}
