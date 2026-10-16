using System.Security.Cryptography;
using System.Text.Json;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

internal sealed class WikiSearchCursor
{
    private const int MaximumEncodedCursorLength = 1024;
    private const int MaximumContinuationTokenLength = 128;

    public string Scope { get; init; }
    public int Offset { get; init; }
    public string Token { get; init; }

    public WikiSearchCursor(string scope, int offset, string token)
    {
        Scope = scope;
        Offset = offset;
        Token = token;
    }

    public static string GetScope(string searchTerm, string languageCode, int limit)
    {
        byte[] values = JsonSerializer.SerializeToUtf8Bytes(new { searchTerm, languageCode, limit });
        return Convert.ToHexString(SHA256.HashData(values));
    }

    public static WikiSearchCursor? Parse(CursorPageRequest request, string scope)
    {
        if (request.Cursor == null)
        {
            return null;
        }

        try
        {
            if (request.Cursor.Length > MaximumEncodedCursorLength)
            {
                throw new FormatException();
            }

            var cursor = JsonSerializer.Deserialize<WikiSearchCursor>(Convert.FromBase64String(request.Cursor));
            if (cursor == null || cursor.Scope != scope || cursor.Offset <= 0 ||
                string.IsNullOrEmpty(cursor.Token) || cursor.Token.Length > MaximumContinuationTokenLength)
            {
                throw new FormatException();
            }

            return cursor;
        }
        catch (FormatException)
        {
            throw new RequestValidationException("The Wiki cursor is invalid for this query.");
        }
        catch (JsonException)
        {
            throw new RequestValidationException("The Wiki cursor is invalid for this query.");
        }
    }

    public static string Encode(string scope, WikiContinuation continuation, int previousOffset)
    {
        if (continuation.SearchOffset == null || continuation.SearchOffset <= previousOffset ||
            string.IsNullOrEmpty(continuation.Token) || continuation.Token.Length > MaximumContinuationTokenLength)
        {
            throw new InvalidOperationException("Wikipedia returned invalid search continuation data.");
        }

        var cursor = new WikiSearchCursor(scope, continuation.SearchOffset.Value, continuation.Token);
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(cursor));
    }
}
