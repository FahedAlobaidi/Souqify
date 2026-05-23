using Microsoft.Extensions.Primitives;

namespace Souqify.Middlewares
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            httpContext.Response.Headers["X-Frame-Options"] = "DENY";

            await _next(httpContext);
        }
    }
}
