namespace ZooFinder.Application.Common.Pagination.Contracts;

public sealed record CursorPageRequest(
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);
