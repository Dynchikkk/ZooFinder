using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;
using ZooFinder.Application.Common.Animals.Exceptions;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Exceptions;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Api.ErrorHandling;

internal static class ApiExceptionMapper
{
    private const string ServiceUnavailableDetail = "An external service is temporarily unavailable. Please try again later.";
    private const string BadRequestDetail = "The HTTP request is invalid.";

    private static readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> ResponseHeaders =
        new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [StatusCodes.Status401Unauthorized] = new Dictionary<string, string>
            {
                [HeaderNames.WWWAuthenticate] = JwtBearerDefaults.AuthenticationScheme
            }
        };

    private static readonly IReadOnlyDictionary<Type, Func<Exception, ApiErrorResponse>> ExceptionResponses =
        new Dictionary<Type, Func<Exception, ApiErrorResponse>>
        {
            [typeof(RequestValidationException)] = exception => CreateResponse(StatusCodes.Status400BadRequest, exception.Message),
            [typeof(UnauthorizedException)] = exception => CreateResponse(StatusCodes.Status401Unauthorized, exception.Message),
            [typeof(ForbiddenException)] = exception => CreateResponse(StatusCodes.Status403Forbidden, exception.Message),
            [typeof(NotFoundException)] = exception => CreateResponse(StatusCodes.Status404NotFound, exception.Message),
            [typeof(ConflictException)] = exception => CreateResponse(StatusCodes.Status409Conflict, exception.Message),
            [typeof(AnimalInformationUnavailableException)] = _ => CreateResponse(StatusCodes.Status503ServiceUnavailable, ServiceUnavailableDetail),
            [typeof(AnimalRecognitionUnavailableException)] = _ => CreateResponse(StatusCodes.Status503ServiceUnavailable, ServiceUnavailableDetail),
            [typeof(BadHttpRequestException)] = exception => CreateResponse(((BadHttpRequestException)exception).StatusCode, BadRequestDetail)
        };

    public static ApiErrorResponse Map(Exception exception)
    {
        Func<Exception, ApiErrorResponse> createResponse = ExceptionResponses
            .FirstOrDefault(entry => entry.Key.IsInstanceOfType(exception)).Value ?? CreateUnexpectedResponse;
        return createResponse(exception);
    }

    private static ApiErrorResponse CreateResponse(int statusCode, string? detail = null)
    {
        return new ApiErrorResponse(statusCode, detail, ResponseHeaders.GetValueOrDefault(statusCode));
    }

    private static ApiErrorResponse CreateUnexpectedResponse(Exception _)
    {
        return CreateResponse(StatusCodes.Status500InternalServerError);
    }
}
