namespace Souqify.Services.Interfaces
{
    public interface IGuestCookieService
    {
        Guid GetGuestId(HttpContext httpContext);

        bool ClearCookie(HttpContext httpContext);
    }
}
