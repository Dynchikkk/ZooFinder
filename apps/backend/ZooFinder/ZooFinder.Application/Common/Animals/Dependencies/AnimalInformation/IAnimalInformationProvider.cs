using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;

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
