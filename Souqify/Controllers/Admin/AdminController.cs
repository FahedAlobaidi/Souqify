using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Category;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Interfaces;
using Souqify.Controllers.Customer;
using Souqify.Domain;

namespace Souqify.Controllers.Admin
{
    [Route("api/admin")]
    [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme,Policy ="MustBeAdmin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public AdminController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        [HttpGet("products/low-stock")]
        public async Task<ActionResult<IEnumerable<LowStockDto>>> GetLowStock()
        {
            return Ok(await _productService.GetLowStockVariantsAsync());
        }

        [HttpPost("categories")]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto createCategoryDto)
        {
            var category = await _categoryService.AddCategoryAsync(createCategoryDto);
            //note: change this to createdAtAction
            return Ok(category);
        }

        [HttpPost("products")]
        public async Task<ActionResult<AdminProductDto>> CreateProduct(CreateProductDto createProductDto)
        {
            var adminProduct = await _productService.CreateProductAsync(createProductDto);

            return CreatedAtAction(
                actionName: nameof(ProductController.GetProductById),
                controllerName: "Product",
                routeValues: new { productId = adminProduct.Id },
                value: adminProduct
            );
        }

        [HttpPut("products/{productId}")]
        public async Task<ActionResult<AdminProductDto>> UpdateProduct(Guid productId, UpdateProductDto updateProductDto)
        {
            var updatedProductAdmin = await _productService.UpdateProductAsync(productId, updateProductDto);

            return Ok(updatedProductAdmin);
        }

        [HttpPatch("products/{productId}/status")]
        public async Task<ActionResult<bool>> ToggleProductStatus(Guid productId)
        {
            return Ok(await _productService.ToggleProductStatusAsync(productId));
        }

        [HttpPost("products/{productId}/variants")]
        public async Task<ActionResult<AdminVariantDto>> CreateVariant(Guid productId,CreateVariantDto createVariantDto)
        {
            var adminVariant = await _productService.AddVariantAsync(productId, createVariantDto);

            return CreatedAtAction(
                actionName: nameof(ProductController.GetProductById),
                controllerName: "Product",
                routeValues: new { productId = productId },
                value: adminVariant
                );
        }

        [HttpPut("products/{productId}/variants/{variantId}")]
        public async Task<ActionResult<AdminVariantDto>> UpdateVariant(Guid productId,Guid variantId,UpdateVariantDto updateVariantDto)
        {
            var updatedVariant = await _productService.UpdateVariantAsync(productId, variantId, updateVariantDto);

            return Ok(updatedVariant);
        }

        [HttpPost("products/{productId}/images")]
        public async Task<ActionResult<ProductImageDto>> CreateImage(Guid productId,CreateImageDto createImageDto)
        {
            var imageDto = await _productService.AddImageAsync(productId,createImageDto);

            return CreatedAtAction(
                actionName: nameof(ProductController.GetProductById),
                controllerName: "Product",
                routeValues: new { productId = productId },
                value: imageDto
                );
        }

        [HttpDelete("products/{productId}/images/{imageId}")]
        public async Task<ActionResult> DeleteImage(Guid productId,Guid imageId)
        {
            await _productService.RemoveImageAsync(productId, imageId);

            return NoContent();
        }

    }
}
