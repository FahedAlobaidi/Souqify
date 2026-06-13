
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Souqify.Application.DTOs.Auth;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
using Souqify.Infrastructure.Identity;
using System.Security.Cryptography.Xml;

namespace Souqify.Infrastructure.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenRepository _refreshToken;
        private readonly SouqifyDbContext _souqifyDbContext;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser>signInManager,SouqifyDbContext souqifyDbContext,IConfiguration configuration, IJwtTokenService jwtTokenService,IRefreshTokenRepository refreshToken)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _souqifyDbContext = souqifyDbContext;
            _configuration = configuration;
            _jwtTokenService = jwtTokenService;
            _refreshToken = refreshToken;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequestDto)
        {

            var user = await _userManager.FindByEmailAsync(loginRequestDto.Email);

            //if (user == null)
            //{
            //    throw new UnauthorizedException("Email or password is wrong");
            //}

            //if (!await _userManager.CheckPasswordAsync(user, loginRequestDto.Password))
            //{
            //    throw new UnauthorizedException("Email or password is wrong");
            //}

            //return await CreateAuthResponse()


            if(user!= null)
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user,loginRequestDto.Password, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    return await CreateAuthResponse(user);
                }
                else if(result.IsLockedOut)
                {
                    throw new LockoutException("Account temporarily locked. Please try again later.");
                }
                else//this will check if the user entered wrong password (if the email was wrong this block of code will never be reached)
                {
                    throw new UnauthorizedException("Email or password is wrong");
                }
            }
            else
            {
                throw new UnauthorizedException("Email or password is wrong");
            }
        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshTokenstring)
        {
            var refreshToken = await _refreshToken.GetRefreshTokenAsync(refreshTokenstring);

            if (refreshToken == null)
            {
                throw new UnauthorizedException("Invalid token");
            }

            if (!refreshToken.IsActive)
            {
                if(refreshToken.ReplacedByToken!= null)
                {
                    await _refreshToken.RevokAllForUserAsync(refreshToken.UserId);
                    await _refreshToken.SaveChanges();
                }

                throw new UnauthorizedException("Invalid token");
            }

            var user = await _userManager.FindByIdAsync((refreshToken.UserId).ToString());

            if (user == null)
            {
                throw new UnauthorizedException("Invalid token");
            }

            var roles = await _userManager.GetRolesAsync(user);

            refreshToken.RevokedAt = DateTime.UtcNow;

            var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

            refreshToken.ReplacedByToken = newRefreshTokenString;

            using var transaction = await _souqifyDbContext.Database.BeginTransactionAsync();

            try
            {
                var newRefreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = refreshToken.UserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Authentication:RefreshTokenExpirationInDays"]!)),
                    Token = newRefreshTokenString
                };

                _refreshToken.AddRefreshTokenAsync(newRefreshToken);

                await _refreshToken.SaveChanges();

                var accessToken = _jwtTokenService.GenerateAccessToken(user.Id,user.Email,roles.ToList());

                var authResponse = new AuthResponseDto { AccessToken = accessToken, RefreshToken = newRefreshToken.Token, ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Authentication:ExpirationInMinutes"]!)) };

                await transaction.CommitAsync();

                return authResponse;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequestDto)
        {

            var user = new ApplicationUser
            {
                Email = registerRequestDto.Email,
                UserName=registerRequestDto.Email,//its required by identity
               // PasswordHash = registerRequestDto.Password, bcs is set by identity inside create async not me
                PhoneNumber = registerRequestDto.PhoneNumber,
                FirstName = registerRequestDto.FirstName,
                LastName = registerRequestDto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            

            //todo:Audit log entry 

            using var transaction = await _souqifyDbContext.Database.BeginTransactionAsync();

            try
            {
                var result = await _userManager.CreateAsync(user, registerRequestDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ",result.Errors.Select(e=>e.Description));

                    throw new BadRequestException(errors);
                }

                var rolesRespose=await _userManager.AddToRoleAsync(user, "Customer");

                if (!rolesRespose.Succeeded)
                {
                    var errors = string.Join("; ", rolesRespose.Errors.Select(e => e.Description));
                    throw new BadRequestException(errors);
                }

                var authResponse= await CreateAuthResponse(user);

                await transaction.CommitAsync();

                return authResponse;
                
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //generate the initial refresh token
        private async Task<AuthResponseDto> CreateAuthResponse(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var token = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles.ToList());

            var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshTokenString,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Authentication:RefreshTokenExpirationInDays"]!))
            };

            _refreshToken.AddRefreshTokenAsync(refreshToken);

            await _refreshToken.SaveChanges();

            var responseDto = new AuthResponseDto { AccessToken = token, RefreshToken = refreshTokenString, ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Authentication:ExpirationInMinutes"]!)) };

            return responseDto;
        }
    }
}
