using System.Security.Cryptography;
using System.Text.Json;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Infrastructure.Persistence.Pagination;

internal sealed record DatabaseCursor(
    int Version,
    string Scope,
    DateTime CreatedAtUtc,
    Guid Id)
{
    public static string GetScope(params string[] values)
    {
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values)));
    }

    public static DatabaseCursor? Parse(
        CursorPageRequest request,
        string scope)
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
            if (request.Cursor.Length > 1024)
            {
                throw new FormatException();
            }

            var cursor = JsonSerializer.Deserialize<DatabaseCursor>(
                Convert.FromBase64String(request.Cursor));
            if (cursor == null || cursor.Version != 1 || cursor.Scope != scope ||
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

    public static string Encode(
        string scope,
        DateTime createdAtUtc,
        Guid id)
    {
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
            new DatabaseCursor(1, scope, DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc), id)));
    }
}
