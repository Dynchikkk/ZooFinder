using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Animals.Catalog.Contracts;

namespace ZooFinder.Application.Features.Animals.Catalog.Interfaces;

public interface IAnimalCatalogService
{
    Task<CursorPageResponse<AnimalCardResponse>> SearchAsync(
        AnimalSearchRequest request,
        CancellationToken cancellationToken);

    Task<AnimalPageResponse> GetAnimalAsync(
        AnimalPageRequest request,
        CancellationToken cancellationToken);
}
