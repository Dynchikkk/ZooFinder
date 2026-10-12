using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Application.Modules.Animals.Modules.Catalog.Services.AnimalCatalog;

public interface IAnimalCatalogService
{
    Task<CursorPageResponse<AnimalCardResponse>> SearchAsync(
        AnimalSearchRequest request,
        CancellationToken cancellationToken);

    Task<AnimalPageResponse> GetAnimalAsync(
        AnimalPageRequest request,
        CancellationToken cancellationToken);
}
