using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Image;
using Souqify.Application.DTOs.Product;
using Souqify.Application.DTOs.Variant;
using Souqify.Application.Interfaces;
using Souqify.Controllers.Customer;
using Souqify.Domain;

namespace Souqify.Controllers.Admin
{
    [Route("api/admin/products")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IProductService _productService;

        public AdminController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<LowStockDto>>> GetLowStock()
        {
            return Ok(await _productService.GetLowStockVariantsAsync());
        }

        [HttpPost]
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

        [HttpPut("{productId}")]
        public async Task<ActionResult<AdminProductDto>> UpdateProduct(Guid productId, UpdateProductDto updateProductDto)
        {
            var updatedProductAdmin = await _productService.UpdateProductAsync(productId, updateProductDto);

            return Ok(updatedProductAdmin);
        }

        [HttpPatch("{productId}/status")]
        public async Task<ActionResult<bool>> ToggleProductStatus(Guid productId)
        {
            return Ok(await _productService.ToggleProductStatusAsync(productId));
        }

        [HttpPost("{productId}/variants")]
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

        [HttpPut("{productId}/variants/{variantId}")]
        public async Task<ActionResult<AdminVariantDto>> UpdateVariant(Guid productId,Guid variantId,UpdateVariantDto updateVariantDto)
        {
            var updatedVariant = await _productService.UpdateVariantAsync(productId, variantId, updateVariantDto);

            return Ok(updatedVariant);
        }

        [HttpPost("{productId}/images")]
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

        [HttpDelete("{productId}/images/{imageId}")]
        public async Task<ActionResult> DeleteImage(Guid productId,Guid imageId)
        {
            await _productService.RemoveImageAsync(productId, imageId);

            return NoContent();
        }

    }
}
