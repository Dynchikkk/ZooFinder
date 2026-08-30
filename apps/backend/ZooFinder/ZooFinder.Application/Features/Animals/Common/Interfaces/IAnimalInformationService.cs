using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Features.Animals.Common.Contracts;

namespace ZooFinder.Application.Features.Animals.Common.Interfaces;

public interface IAnimalInformationService
{
    Task<CursorPageResponse<AnimalInformationSearchResult>> SearchAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken);

    Task<AnimalInformationDetailsResult?> GetDetailsAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken);
}
