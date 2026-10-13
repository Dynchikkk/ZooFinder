using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Parks.Modules.Catalog.Services.Parks;

public interface IParkService
{
    Task<ParkResponse> GetAsync(
        Guid parkId,
        CancellationToken cancellationToken);

    Task<OffsetPageResponse<ParkResponse>> SearchAsync(
        ParkSearchRequest request,
        CancellationToken cancellationToken);

    Task<ParkResponse> CreateAsync(
        CreateParkRequest request,
        CancellationToken cancellationToken);

    Task<ParkResponse> UpdateAsync(
        UpdateParkRequest request,
        CancellationToken cancellationToken);

    Task<ParkResponse> SetStatusAsync(
        Guid parkId,
        ParkStatus status,
        CancellationToken cancellationToken);
}
