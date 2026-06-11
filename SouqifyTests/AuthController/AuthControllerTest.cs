

using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Souqify.Application.DTOs.Auth;
using Souqify.Infrastructure;
using Souqify.Infrastructure.Identity;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace SouqifyTests.AuthController
{
    public class AuthControllerTest:IClassFixture<SouqifyApiFactory>
    {
        private readonly HttpClient _httpClient;
        private readonly SouqifyApiFactory _souqifyApiFactory;

        public AuthControllerTest(SouqifyApiFactory souqifyApiFactory)
        {
            _souqifyApiFactory = souqifyApiFactory;
            _httpClient = _souqifyApiFactory.CreateClient();
        }

        [Fact]
        public async Task Register_ReturnOk()
        {
            var faker = new Faker();

            var user = new RegisterRequestDto
            {
                Email = faker.Person.Email,
                FirstName = faker.Person.FirstName,
                LastName = faker.Person.LastName,
                Password = "Sa123456",
                ConfirmPassword = "Sa123456",
                PhoneNumber ="123456788"
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/register", user);

            var createdUser = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            createdUser.Should().NotBeNull();
            createdUser.AccessToken.Should().NotBeNullOrEmpty();
            createdUser.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_ReturnOk()
        {
            var faker = new Faker();

            var fakeEmail = faker.Person.Email;

            var applicationUser = new ApplicationUser
            {
                Email = fakeEmail,
                UserName= fakeEmail,
                FirstName = faker.Person.FirstName,
                LastName = faker.Person.LastName,
                PhoneNumber = "1112223332"
            };

            using var scope = _souqifyApiFactory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var userManagerResult= await userManager.CreateAsync(applicationUser, "Sa123456");
            if (!userManagerResult.Succeeded)
            {
                var errors = string.Join("; ", userManagerResult.Errors.Select(e => e.Description));

                throw new Exception($"Seed failed:{errors}");
            }

            var roleResult= await userManager.AddToRoleAsync(applicationUser, "Customer");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));

                throw new Exception($"Seed failed:{errors}");
            }

            var loginDto = new LoginRequestDto
            {
                Email = fakeEmail,
                Password = "Sa123456"
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();

        }

        //[Fact]
        //public async Task Refresh_returnOk()
        //{

        //}
    }
}
