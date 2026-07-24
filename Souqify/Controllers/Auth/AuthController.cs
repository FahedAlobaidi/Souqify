using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Souqify.Application.DTOs.Auth;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Souqify.Controllers.Auth
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICartService _cartService;
        private readonly IGuestCookieService _guestCookieService;

        public AuthController(IAuthService authService,ICartService cartService,IGuestCookieService guestCookieService)
        {
            _authService = authService;
            _cartService = cartService;
            _guestCookieService = guestCookieService;
        }
        

        [HttpPost("register")]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerRequestDto)
        {
            var response = await _authService.RegisterAsync(registerRequestDto);

            await _cartService.MergeGuestCartAsync(GetGuestId(), response.UserId);
            _guestCookieService.ClearCookie(HttpContext);

            return Ok(response);
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginRequestDto)
        {
            var response = await _authService.LoginAsync(loginRequestDto);

            await _cartService.MergeGuestCartAsync(GetGuestId(), response.UserId);
            _guestCookieService.ClearCookie(HttpContext);

            return Ok(response);
        }

        [HttpPost("refresh/{refreshTokenString}")]
        [EnableRateLimiting("RefreshLimiter")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(string refreshTokenString)
        {
            var response = await _authService.RefreshAsync(refreshTokenString);

            return Ok(response);
        }

        private Guid GetGuestId()
        {
            return _guestCookieService.GetGuestId(HttpContext);
        }
    }
}
