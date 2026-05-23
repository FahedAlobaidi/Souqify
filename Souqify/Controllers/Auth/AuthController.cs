using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Souqify.Application.DTOs.Auth;
using Souqify.Application.Interfaces;

namespace Souqify.Controllers.Auth
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerRequestDto)
        {
            var response = await _authService.RegisterAsync(registerRequestDto);

            return Ok(response);
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginRequestDto)
        {
            var response = await _authService.LoginAsync(loginRequestDto);

            return Ok(response);
        }

        [HttpPost("refresh/{refreshTokenString}")]
        [EnableRateLimiting("RefreshLimiter")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(string refreshTokenString)
        {
            var response = await _authService.RefreshAsync(refreshTokenString);

            return Ok(response);
        }
    }
}
