namespace ZooFinder.Application.Common.Pagination.Contracts;

public sealed record CursorPageResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor)
{
    public bool HasMore => !string.IsNullOrWhiteSpace(NextCursor);
}
