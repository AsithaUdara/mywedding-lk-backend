using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyWedding.SharedKernel.Exceptions;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyWedding.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            var problemDetails = exception switch
            {
                BaseException baseEx => new ProblemDetails
                {
                    Title = baseEx.Title,
                    Detail = baseEx.Message,
                    Status = baseEx.StatusCode,
                    Instance = context.Request.Path
                },
                _ => new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = context.Request.Path
                }
            };

            // Specialized handling for ValidationException to include error list
            if (exception is ValidationException validationEx)
            {
                problemDetails.Extensions["errors"] = validationEx.Errors;
            }

            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(problemDetails, options);

            await context.Response.WriteAsync(result);
        }
    }
}
