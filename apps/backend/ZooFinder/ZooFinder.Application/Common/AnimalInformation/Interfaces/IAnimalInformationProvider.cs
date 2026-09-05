using ZooFinder.Application.Common.AnimalInformation.Contracts;
using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Application.Common.AnimalInformation.Interfaces;

public interface IAnimalInformationProvider
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
