using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Domain.Animals;

namespace ZooFinder.Application.Features.Animals.Common.Interfaces;

public interface IAnimalRepository
{
    Task<Animal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

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

    Task AddAsync(Animal animal, CancellationToken cancellationToken);

    Task UpdateAsync(Animal animal, CancellationToken cancellationToken);
}
