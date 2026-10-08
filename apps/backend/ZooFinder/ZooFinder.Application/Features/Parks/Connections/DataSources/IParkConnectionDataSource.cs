using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Connections.DataSources;

public interface IParkConnectionDataSource
{
    Task<ParkConnectionRequest?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken);
    Task<OffsetPageResponse<ParkConnectionRequest>> SearchAsync(
        Guid parkId,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken);
    Task<bool> TryAddAsync(
        ParkConnectionRequest request,
        CancellationToken cancellationToken);
    Task<bool> TryUpdateAsync(
        ParkConnectionRequest request,
        ParkConnectionStatus expectedStatus,
        CancellationToken cancellationToken);
}
