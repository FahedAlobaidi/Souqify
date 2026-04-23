using Microsoft.AspNetCore.Mvc;
using Souqify.Application.Exceptions;

namespace Souqify.Middlewares
{
    public class GlobalExceptionHandliongMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionHandliongMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(BadRequestException ex)
            {
                context.Response.StatusCode = 400;

                var problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = ex.Message
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(problemDetails);
            }catch(NotFoundException ex)
            {
                context.Response.StatusCode = 404;

                var problemDetails = new ProblemDetails
                {
                    Status = 404,
                    Title = "Not Found",
                    Detail = ex.Message,
                    
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                var problemDetails = new ProblemDetails
                {
                    Status = 500,
                    Title = "Internal Server Error",
                    Detail = "Something went wrong"
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        }
    }
}
