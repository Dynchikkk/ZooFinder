using ZooFinder.Application.Shared.Pagination.Constants;

namespace ZooFinder.Application.Shared.Pagination.Contracts;

public sealed record CursorPageRequest(
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);
