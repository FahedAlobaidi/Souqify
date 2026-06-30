
using Microsoft.AspNetCore.Mvc;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.Interfaces;

namespace Souqify.Controllers.Customer
{
    [Route("api/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("{customerId}")]
        public async Task<ActionResult<CartDto>> GetCartAsync(Guid customerId)
        {
            var cartDto = await _cartService.GetCartAsync(customerId, false);

            return Ok(cartDto);
        }

        [HttpPost("{customerId}")]
        public async Task<ActionResult<CartDto>> CreateCartAsync(Guid customerId, CreateCartDto createCartDto)
        {
            var cartDto = await _cartService.AddCartAsync(customerId, createCartDto);

            return Ok(cartDto);
        }

        [HttpPatch("{customerId}/{cartItemId}")]
        public async Task<ActionResult<CartDto>> DecreaseCartItemAsync(Guid customerId, Guid cartItemId)
        {
            var cartDto = await _cartService.DecreaseCartItemQuantityAsync(customerId, cartItemId);

            return Ok(cartDto);
        }


        [HttpDelete("{customerId}/{cartItemId}")]
        public async Task<ActionResult<CartDto>> DeleteCartItemAsync(Guid customerId, Guid cartItemId)
        {
            return Ok(await _cartService.DeleteCartItemAsync(customerId, cartItemId));
        }

        [HttpDelete("{customerId}")]
        public async Task<ActionResult<CartDto>> DeleteCartAsync(Guid customerId)
        {
            return Ok(await _cartService.DeleteCartAsync(customerId));
        }
    }
}
