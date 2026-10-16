using System.Security.Cryptography;
using System.Text.Json;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Constants;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Infrastructure.Persistence.Pagination;

internal sealed class DatabaseCursor
{
    private const int MaximumEncodedCursorLength = 1024;

    public string Scope { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public Guid Id { get; init; }

    public DatabaseCursor(string scope, DateTime createdAtUtc, Guid id)
    {
        Scope = scope;
        CreatedAtUtc = createdAtUtc;
        Id = id;
    }

    public static string GetScope(params string[] values)
    {
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values)));
    }

    public static DatabaseCursor? Parse(CursorPageRequest request, string scope)
    {
        if (request.Limit < 1 || request.Limit > PaginationDefaults.MaximumPageSize)
        {
            throw new RequestValidationException("Page size is out of range.");
        }

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

            var cursor = JsonSerializer.Deserialize<DatabaseCursor>(Convert.FromBase64String(request.Cursor));
            if (cursor == null || cursor.Scope != scope ||
                cursor.Id == Guid.Empty || cursor.CreatedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new FormatException();
            }

            return cursor;
        }
        catch (FormatException)
        {
            throw new RequestValidationException("The cursor is invalid for this query.");
        }
        catch (JsonException)
        {
            throw new RequestValidationException("The cursor is invalid for this query.");
        }
    }

    public static string Encode(string scope, DateTime createdAtUtc, Guid id)
    {
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
            new DatabaseCursor(scope, DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc), id)));
    }
}
