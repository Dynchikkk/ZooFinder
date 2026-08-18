using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Features.Animals.Common.Contracts;

namespace ZooFinder.Application.Features.Animals.Common.Interfaces;

public interface IAnimalInformationService
{
    Task<CursorPageResponse<AnimalInformationSearchItem>> SearchAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken);
}
