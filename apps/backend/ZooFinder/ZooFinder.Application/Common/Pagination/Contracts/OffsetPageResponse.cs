namespace ZooFinder.Application.Common.Pagination.Contracts;

public sealed record OffsetPageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);
