namespace ZooFinder.Application.Common.Contracts.Pagination;

public sealed record CursorPageRequest(
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);
