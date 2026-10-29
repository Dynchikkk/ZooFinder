using Microsoft.AspNetCore.Diagnostics;
using ZooFinder.Application.Common.Animals.Exceptions;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Exceptions;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Api.ErrorHandling;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    private static readonly IReadOnlyDictionary<Type, int> ExceptionStatusCodes = new Dictionary<Type, int>
    {
        [typeof(RequestValidationException)] = StatusCodes.Status400BadRequest,
        [typeof(UnauthorizedException)] = StatusCodes.Status401Unauthorized,
        [typeof(ForbiddenException)] = StatusCodes.Status403Forbidden,
        [typeof(NotFoundException)] = StatusCodes.Status404NotFound,
        [typeof(ConflictException)] = StatusCodes.Status409Conflict,
        [typeof(AnimalInformationUnavailableException)] = StatusCodes.Status503ServiceUnavailable,
        [typeof(AnimalRecognitionUnavailableException)] = StatusCodes.Status503ServiceUnavailable
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode = GetStatusCode(exception);
        IResult result = Results.Problem(
            statusCode: statusCode,
            detail: GetPublicDetail(exception, statusCode),
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier });
        await result.ExecuteAsync(httpContext);
        return true;
    }

    private static int GetStatusCode(Exception exception)
    {
        var badRequest = exception as BadHttpRequestException;
        if (badRequest != null)
        {
            return badRequest.StatusCode;
        }

        return ExceptionStatusCodes.GetValueOrDefault(exception.GetType(), StatusCodes.Status500InternalServerError);
    }

    private static string? GetPublicDetail(Exception exception, int statusCode)
    {
        if (statusCode == StatusCodes.Status503ServiceUnavailable)
        {
            return "An external service is temporarily unavailable. Please try again later.";
        }

        if (exception as BadHttpRequestException != null)
        {
            return "The HTTP request is invalid.";
        }

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            return null;
        }

        return exception.Message;
    }
}
