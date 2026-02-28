using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyWedding.Application.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyWedding.API.Middleware
{
    /// <summary>
    /// Centralized exception handling middleware for consistent API error responses.
    /// Handles application-specific and unhandled system exceptions.
    /// </summary>
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var result = string.Empty;

            switch (exception)
            {
                case BadRequestException badRequestException:
                    code = HttpStatusCode.BadRequest;
                    result = JsonSerializer.Serialize(new { message = badRequestException.Message });
                    break;
                case NotFoundException notFoundException:
                    code = HttpStatusCode.NotFound;
                    result = JsonSerializer.Serialize(new { message = notFoundException.Message });
                    break;
                case ForbiddenAccessException forbiddenException:
                    code = HttpStatusCode.Forbidden;
                    result = JsonSerializer.Serialize(new { message = forbiddenException.Message });
                    break;
                default:
                    // Generic internal server error
                    result = JsonSerializer.Serialize(new { message = "An unexpected error occurred on the server.", detail = exception.Message });
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            return context.Response.WriteAsync(result);
        }
    }
}
