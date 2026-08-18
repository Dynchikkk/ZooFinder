namespace ZooFinder.Application.Common.Extensions;

public static class PaginationExtensions
{
    public static string? NormalizeCursor(this string? cursor)
    {
        return string.IsNullOrWhiteSpace(cursor) ? null : cursor.Trim();
    }
}
