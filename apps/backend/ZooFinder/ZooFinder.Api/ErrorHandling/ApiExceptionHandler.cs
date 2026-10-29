using Microsoft.AspNetCore.Diagnostics;

namespace ZooFinder.Api.ErrorHandling;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ApiErrorResponse error = ApiExceptionMapper.Map(exception);
        foreach (var header in error.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value;
        }

        IResult result = Results.Problem(
            statusCode: error.StatusCode,
            detail: error.Detail,
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier });
        await result.ExecuteAsync(httpContext);
        return true;
    }
}
