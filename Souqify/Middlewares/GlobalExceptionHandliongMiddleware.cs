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

                var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = ex.Message
                });

                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(problemDetails);
            }catch(NotFoundException ex)
            {
                context.Response.StatusCode = 404;

                var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
                {
                    Status = 404,
                    Title = "Not Found",
                    Detail = ex.Message,
                    
                });

                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(problemDetails);
            }
            catch (UnauthorizedException ex)
            {
                context.Response.StatusCode = 401;
                var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
                {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = ex.Message
                });

                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(problemDetails);
            }
            catch(LockoutException ex)
            {
                context.Response.StatusCode = 423;
                var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
                {
                    Status = 423,
                    Title = "Louckedout",
                    Detail = ex.Message
                });
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(problemDetails);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
                {
                    Status = 500,
                    Title = "Server error",
                    Detail = "Something went wrong"
                });
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(problemDetails);
            }
        }
    }
}
