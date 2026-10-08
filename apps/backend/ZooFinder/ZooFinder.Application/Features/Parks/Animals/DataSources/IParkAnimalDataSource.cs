using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Animals.DataSources;

public interface IParkAnimalDataSource
{
    // Reads include the shared Animal. Search hides unpublished links by default;
    // direct Get supports editing an unpublished association.
    Task<ParkAnimal?> GetAsync(
        Guid parkAnimalId,
        CancellationToken cancellationToken);
    Task<OffsetPageResponse<ParkAnimal>> SearchAsync(
        Guid parkId,
        bool includeUnpublished,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken);
    Task<ParkAnimal> UpsertAsync(
        ParkAnimal parkAnimal,
        CancellationToken cancellationToken);
    // Atomic only for park links; shared species registration happens before this operation.
    Task<IReadOnlyList<ParkAnimal>> UpsertBatchAsync(
        Guid parkId,
        IReadOnlyList<ParkAnimal> parkAnimals,
        CancellationToken cancellationToken);
    Task UpdateAsync(
        ParkAnimal parkAnimal,
        CancellationToken cancellationToken);
    Task RemoveAsync(
        Guid parkAnimalId,
        CancellationToken cancellationToken);
}
