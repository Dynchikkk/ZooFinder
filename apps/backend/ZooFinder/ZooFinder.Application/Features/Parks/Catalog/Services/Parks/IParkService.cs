using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Catalog.Services.Parks;

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
