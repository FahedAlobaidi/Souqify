

using Bogus;
using FluentAssertions;
using Souqify.Application.DTOs.Auth;
using System.Net.Http.Json;

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
    }
}
