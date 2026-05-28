using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyWedding.SharedKernel.Exceptions;

namespace MyWedding.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns RFC 7807 Problem Details (application/problem+json).
/// </summary>
public class ExceptionMiddleware
{
    private const string ProblemJson = "application/problem+json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title, type, detail) = MapException(exception);

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = $"{context.Request.Method} {context.Request.Path}",
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (exception is ValidationException validationEx)
        {
            problem.Extensions["errors"] = validationEx.Errors;
        }

        if (_environment.IsDevelopment() && status >= 500)
        {
            problem.Extensions["exception"] = exception.GetType().Name;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = ProblemJson;
        context.Response.StatusCode = status;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int Status, string Title, string Type, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validation => (
                validation.StatusCode,
                validation.Title,
                ProblemTypeUri(validation.StatusCode, "validation-error"),
                validation.Message),

            BaseException baseEx => (
                baseEx.StatusCode,
                baseEx.Title,
                ProblemTypeUri(baseEx.StatusCode, SlugFromTitle(baseEx.Title)),
                baseEx.Message),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                ProblemTypeUri(StatusCodes.Status403Forbidden, "forbidden"),
                exception.Message),

            ArgumentException arg => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                ProblemTypeUri(StatusCodes.Status400BadRequest, "bad-request"),
                arg.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                ProblemTypeUri(StatusCodes.Status500InternalServerError, "internal-server-error"),
                "An unexpected error occurred. Please try again later."),
        };
    }

    private static string ProblemTypeUri(int status, string slug) =>
        $"https://api.mywedding.lk/problems/{slug}#status-{status}";

    private static string SlugFromTitle(string title) =>
        title.Trim().ToLowerInvariant().Replace(' ', '-');
}
