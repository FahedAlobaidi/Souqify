
using Souqify.Application.DTOs.Auth;
using Souqify.Domain.Entities;

namespace Souqify.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequestDto);
        Task<AuthResponseDto> RefreshAsync(string refreshTokenString);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequestDto);
    }
}
