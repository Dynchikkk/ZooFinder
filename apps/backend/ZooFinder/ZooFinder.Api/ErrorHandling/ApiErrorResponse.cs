namespace ZooFinder.Api.ErrorHandling;

internal sealed class ApiErrorResponse
{
    public int StatusCode { get; }
    public string? Detail { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }

    public ApiErrorResponse(int statusCode, string? detail, IReadOnlyDictionary<string, string>? headers)
    {
        StatusCode = statusCode;
        Detail = detail;
        Headers = headers ?? new Dictionary<string, string>();
    }
}
