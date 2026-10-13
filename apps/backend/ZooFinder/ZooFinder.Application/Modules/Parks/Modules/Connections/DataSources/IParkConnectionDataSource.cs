using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Parks.Modules.Connections.DataSources;

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
