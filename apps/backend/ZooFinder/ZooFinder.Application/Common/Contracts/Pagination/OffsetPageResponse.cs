namespace ZooFinder.Application.Common.Contracts.Pagination;

public sealed record OffsetPageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);
