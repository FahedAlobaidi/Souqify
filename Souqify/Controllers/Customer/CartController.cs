
using Microsoft.AspNetCore.Mvc;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Souqify.Controllers.Customer
{
    [Route("api/carts")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IGuestCookieService _guestCookieService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService,IGuestCookieService guestCookieService,ILogger<CartController> logger)
        {
            _cartService = cartService;
            _guestCookieService = guestCookieService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCartAsync()
        {

            var cart = await ResolveCartAync(_cartService.GetUserCartAsync, _cartService.GetGuestCartAsync);

            return Ok(cart);
        }

        

        [HttpPost]
        public async Task<ActionResult<CartDto>> CreateCartAsync( CreateCartDto createCartDto)
        {

            var cart = await ResolveCartAync(id => _cartService.AddUserCartAsync(id, createCartDto), id => _cartService.AddGuestCartAsync(id, createCartDto));
            
            return Ok(cart);
        }

        

        [HttpPatch("{cartItemId}")]
        public async Task<ActionResult<CartDto>> DecreaseCartItemAsync( Guid cartItemId)
        {

            var cart = await ResolveCartAync(id => _cartService.DecreaseUserCartItemsQuantityAsync(id, cartItemId), id => _cartService.DecreaseGuestCartItemQuantityAsync(id, cartItemId));

            return Ok(cart);
        }

        

        [HttpDelete("{cartItemId}")]
        public async Task<ActionResult<CartDto>> DeleteCartItemAsync( Guid cartItemId)
        {

            var cart = await ResolveCartAync(id => _cartService.DeleteUserCartItemAsync(id, cartItemId), id => _cartService.DeleteGuestCartItemAsync(id, cartItemId));

            return Ok(cart);
        }

        [HttpDelete]
        public async Task<ActionResult<CartDto>> DeleteCartAsync()
        {

            var cart = await ResolveCartAync( _cartService.DeleteUserCartAsync, _cartService.DeleteGuestCartAsync);

            return Ok(cart);

        }

        private async Task<CartDto> ResolveCartAync(Func<Guid, Task<CartDto>> userAction, Func<Guid, Task<CartDto>> guestAction)
        {
            //var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            //var isAuth = HttpContext.User.Identity?.IsAuthenticated;
            //_logger.LogWarning("Auth Header: {Heder} | IsAuth:{IsAuth}",authHeader,isAuth);

            //foreach (var c in HttpContext.User.Claims)
            //    _logger.LogWarning("CLAIM {Type} = {Value}", c.Type, c.Value);

            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                return await userAction(GetUserId());
            }
            else
            {
                return await guestAction(GetGuestId());
            }
        }

        private Guid GetUserId()
        {
            var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ??HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (claim == null || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedException("Invalid or missing user id in token");

            return id;
        }

        private Guid GetGuestId()
        {
            return _guestCookieService.GetGuestId(HttpContext);
        }
        
    }
}
