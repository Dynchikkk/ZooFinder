using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Catalog.DataSources;

public interface IParkCatalogDataSource
{
    Task<Park?> GetAsync(
        Guid parkId,
        CancellationToken cancellationToken);
    Task<OffsetPageResponse<Park>> SearchAsync(
        string searchTerm,
        bool includeSuspended,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken);
    Task<bool> TryAddAsync(
        Park park,
        CancellationToken cancellationToken);
    Task<bool> TryUpdateAsync(
        Park park,
        CancellationToken cancellationToken);
}
