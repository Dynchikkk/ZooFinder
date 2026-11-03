using Microsoft.AspNetCore.Diagnostics;

namespace ZooFinder.Api.Logging;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = httpContext.TraceIdentifier
        });

        await _next(httpContext);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "HTTP {Method} {RouteTemplate} returned {StatusCode}",
                httpContext.Request.Method, GetRouteTemplate(httpContext), httpContext.Response.StatusCode);
        }
    }

    private static string GetRouteTemplate(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint() ?? httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint;
        var routeEndpoint = endpoint as RouteEndpoint;
        return routeEndpoint?.RoutePattern.RawText ?? "unmatched";
    }
}
