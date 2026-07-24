using Microsoft.AspNetCore.DataProtection;
using Souqify.Services.Interfaces;
using System.Security.Cryptography;

namespace Souqify.Services
{
    public sealed class GuestCookieService : IGuestCookieService
    {
        private const string _cookieName = "GuestCookie";
        private readonly IDataProtector _dataProtector;
        private readonly ILogger<GuestCookieService> _logger;

        public GuestCookieService(IDataProtectionProvider dataProtector, ILogger<GuestCookieService> logger)
        {
            _dataProtector = dataProtector.CreateProtector("Souqify.GuestId.v1");
            _logger = logger;
        }

        private Guid SetGuestId(HttpContext httpContext)
        {
            var guestId = Guid.NewGuid();

            var protectedGuestId = _dataProtector.Protect(guestId.ToString());

            httpContext.Response.Cookies.Append(_cookieName, protectedGuestId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return guestId;
        }

        public Guid GetGuestId(HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.TryGetValue(_cookieName, out var protectedGuestId))
                return SetGuestId(httpContext);

            try
            {
                var guestIdString = _dataProtector.Unprotect(protectedGuestId);
                if (!Guid.TryParse(guestIdString, out var guestId))
                    return SetGuestId(httpContext);

                return guestId;
            }
            catch(CryptographicException)
            {
                _logger.LogWarning("guest cookie could not be read, making a fresh one");
                return SetGuestId(httpContext);
            }

        }

        public bool ClearCookie(HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.TryGetValue(_cookieName, out var guestId))
                return false;

            httpContext.Response.Cookies.Delete(_cookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            });

            return true;
        }
    }
}
