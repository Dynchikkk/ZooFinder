using ZooFinder.Application.Shared.Pagination.Constants;

namespace ZooFinder.Application.Shared.Pagination.Contracts;

public sealed record OffsetPageRequest(
    int Page = PaginationDefaults.FirstPage,
    int PageSize = PaginationDefaults.DefaultPageSize);
