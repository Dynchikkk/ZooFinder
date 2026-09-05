using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Animals;

namespace ZooFinder.Application.Features.Animals.Catalog.Interfaces;

public interface IAnimalCatalogRepository
{
    Task<Animal?> GetBySourceItemAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken);

    Task<CursorPageResponse<Animal>> SearchAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken);
}
