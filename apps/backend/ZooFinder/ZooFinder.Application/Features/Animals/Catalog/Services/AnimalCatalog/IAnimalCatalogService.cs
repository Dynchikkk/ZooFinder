using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Application.Features.Animals.Catalog.Services.AnimalCatalog;

public interface IAnimalCatalogService
{
    Task<CursorPageResponse<AnimalCardResponse>> SearchAsync(
        AnimalSearchRequest request,
        CancellationToken cancellationToken);

    Task<AnimalPageResponse> GetAnimalAsync(
        AnimalPageRequest request,
        CancellationToken cancellationToken);
}
