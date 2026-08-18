namespace ZooFinder.Application.Common.Contracts.Pagination;

public sealed record OffsetPageRequest(
    int Page = PaginationDefaults.FirstPage,
    int PageSize = PaginationDefaults.DefaultPageSize);
