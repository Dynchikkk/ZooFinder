using ZooFinder.Application.Common.Pagination.Constants;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Catalog.Services.Parks;

public sealed record CreateParkRequest(string Name, string Slug, string? Description = null, string? Address = null);

public sealed record ParkResponse(Guid Id, string Name, string Slug, string? Description, string? Address, ParkStatus Status);

public sealed record ParkSearchRequest(string? SearchTerm = null, bool IncludeSuspended = false,
    int Page = 0, int PageSize = PaginationDefaults.DefaultPageSize);

public sealed record UpdateParkRequest(Guid ParkId, string Name, string Slug, string? Description = null, string? Address = null);
