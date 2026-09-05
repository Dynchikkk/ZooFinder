namespace ZooFinder.Application.Common.Pagination.Contracts;

public sealed record OffsetPageRequest(
    int Page = PaginationDefaults.FirstPage,
    int PageSize = PaginationDefaults.DefaultPageSize);
