using Microsoft.AspNetCore.Mvc;
using Souqify.Application.Exceptions;
using System.Text.Json;

namespace Souqify.Middlewares
{
    public class GlobalExceptionHandliongMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandliongMiddleware> _logger;

        public GlobalExceptionHandliongMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandliongMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            try
            {
                await _next(context);
            }
            catch(Exception ex)
            {
                var (status, title) = ex switch
                {
                    BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
                    NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                    UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                    LockoutException => (StatusCodes.Status423Locked, "Account locked"),
                    _ => (StatusCodes.Status500InternalServerError, "Something went wrong")
                };


                if (status >= 500)
                {
                    _logger.LogError(ex, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);
                }
                else
                {
                    _logger.LogWarning(ex, "Handled {Exception} on {Path}", ex.GetType().Name, context.Request.Path);
                }

                context.Response.StatusCode = status;

                var problemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = status >= 500 ? "Something went wrong" : ex.Message,
                    Extensions = { ["traceId"] = context.TraceIdentifier }

                };

                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            }



            ///this is the old way its has so many duplicated code thats why i make is as above

            //try
            //{
            //    await _next(context);
            //}
            //catch(BadRequestException ex)
            //{
            //    context.Response.StatusCode = 400;

            //    var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
            //    {
            //        Status = 400,
            //        Title = "Bad Request",
            //        Detail = ex.Message
            //    });

            //    context.Response.ContentType = "application/problem+json";
            //    await context.Response.WriteAsync(problemDetails);
            //}catch(NotFoundException ex)
            //{
            //    context.Response.StatusCode = 404;

            //    var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
            //    {
            //        Status = 404,
            //        Title = "Not Found",
            //        Detail = ex.Message,
                    
            //    });

            //    context.Response.ContentType = "application/problem+json";
            //    await context.Response.WriteAsync(problemDetails);
            //}
            //catch (UnauthorizedException ex)
            //{
            //    context.Response.StatusCode = 401;
            //    var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
            //    {
            //        Status = 401,
            //        Title = "Unauthorized",
            //        Detail = ex.Message
            //    });

            //    context.Response.ContentType = "application/problem+json";
            //    await context.Response.WriteAsync(problemDetails);
            //}
            //catch(LockoutException ex)
            //{
            //    context.Response.StatusCode = 423;
            //    var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
            //    {
            //        Status = 423,
            //        Title = "Louckedout",
            //        Detail = ex.Message
            //    });
            //    context.Response.ContentType = "application/problem+json";
            //    await context.Response.WriteAsync(problemDetails);
            //}
            //catch (Exception)
            //{
            //    context.Response.StatusCode = 500;
            //    var problemDetails = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
            //    {
            //        Status = 500,
            //        Title = "Server error",
            //        Detail = "Something went wrong"
            //    });
            //    context.Response.ContentType = "application/problem+json";
            //    await context.Response.WriteAsync(problemDetails);
            //}
        }
    }
}
