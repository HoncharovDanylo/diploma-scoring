using Microsoft.Extensions.Options;

namespace Origination.Api.Middleware;

public sealed class InternalApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly InternalApiOptions _options;

    public InternalApiKeyMiddleware(RequestDelegate next, IOptions<InternalApiOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/internal"))
        {
            if (!context.Request.Headers.TryGetValue("X-Internal-Api-Key", out var key) ||
                !string.Equals(key, _options.ApiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid or missing internal API key.");
                return;
            }
        }

        await _next(context);
    }
}
