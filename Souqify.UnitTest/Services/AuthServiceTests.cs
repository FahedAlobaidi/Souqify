
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Souqify.Application.DTOs.Auth;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Infrastructure;
using Souqify.Infrastructure.Auth;
using Souqify.Infrastructure.Identity;
using Souqify.UnitTest.Fakes;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;


namespace Souqify.UnitTest.Services
{
    public class AuthServiceTests
    {

        [Theory]
        [MemberData(nameof(GetUserChoiceTestData))]
        public async Task LoginAsync_wrongInput_ThrowUnauthorizedException(LoginRequestDto loginRequestDto)
        {
            //arrange
            var fakeUserManager = new Mock<FakeUserManager>();
            var fakeSignInManager = new Mock<FakeSignInManager>();

            //var loginDto = new LoginRequestDto
            //{
            //    Email = "test@gmail.com",
            //    Password = "sa123456"
            //};

            var options = new DbContextOptionsBuilder<SouqifyDbContext>()
                .UseInMemoryDatabase("test")   // or just .Options with no provider
                .Options;
            var dbContext = new SouqifyDbContext(options);

            var authService = new AuthService(fakeUserManager.Object, fakeSignInManager.Object, dbContext, new Mock<IConfiguration>().Object, new Mock<IJwtTokenService>().Object, new Mock<IRefreshTokenRepository>().Object);

            //Act
            var response= async()=> await authService.LoginAsync(loginRequestDto);

            await response.Should().ThrowAsync<UnauthorizedException>();

        }

        [Fact]
        public async Task LoginAsync_Lockout_ThrowLockoutException()
        {
            //arrange
            var fakeUserManager = new Mock<FakeUserManager>();
            var fakeSignInManger = new Mock<FakeSignInManager>();

            fakeSignInManger.Setup(x => x.CheckPasswordSignInAsync(
                    It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync(SignInResult.LockedOut);

            fakeUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationUser { Email = "fahedalobaidi1999@gmail.com" });

            var loginDto = new LoginRequestDto
            {
                Email = "fahedalobaidi1999@gmail.com",
                Password = "sa123456"
            };

            var options = new DbContextOptionsBuilder<SouqifyDbContext>()
                .UseInMemoryDatabase("test")   // or just .Options with no provider
                .Options;
            var dbContext = new SouqifyDbContext(options);

            var authService = new AuthService(fakeUserManager.Object, fakeSignInManger.Object, dbContext, new Mock<IConfiguration>().Object, new Mock<IJwtTokenService>().Object, new Mock<IRefreshTokenRepository>().Object);

            //act
            var result = async () => await authService.LoginAsync(loginDto);

            await result.Should().ThrowAsync<LockoutException>();
        }

        public static IEnumerable<object[]> GetUserChoiceTestData()
        {
            var loginDto = new LoginRequestDto { Email = "hamzaalobaide@gmail.com", Password = "sa123456" };

            yield return new object[] { loginDto };
        }

    }
}
