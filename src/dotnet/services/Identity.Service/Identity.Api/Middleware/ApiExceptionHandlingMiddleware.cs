using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
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
            var (status, title, detail) = ex switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Validation error", ex.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", ex.Message),
                InvalidOperationException => (StatusCodes.Status400BadRequest, "Business rule violation", ex.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Server error", "Unexpected server error.")
            };

            if (status >= 500)
                _logger.LogError(ex, "Unhandled server exception");
            else
                _logger.LogWarning(ex, "Handled application exception");

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var pd = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            };
            await context.Response.WriteAsJsonAsync(pd);
        }
    }
}
