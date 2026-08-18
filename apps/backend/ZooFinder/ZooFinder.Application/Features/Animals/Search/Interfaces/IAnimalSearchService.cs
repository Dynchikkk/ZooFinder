using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Features.Animals.Search.Contracts;

namespace ZooFinder.Application.Features.Animals.Search.Interfaces;

public interface IAnimalSearchService
{
    Task<CursorPageResponse<AnimalSearchItemResponse>> SearchAsync(AnimalSearchRequest request, CancellationToken cancellationToken);
}
