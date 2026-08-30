using ZooFinder.Application.Common.Contracts.Pagination;
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
