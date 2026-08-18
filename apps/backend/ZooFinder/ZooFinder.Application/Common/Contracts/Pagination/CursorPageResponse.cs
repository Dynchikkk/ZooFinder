namespace ZooFinder.Application.Common.Contracts.Pagination;

public sealed record CursorPageResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor)
{
    public bool HasMore => !string.IsNullOrWhiteSpace(NextCursor);
}
