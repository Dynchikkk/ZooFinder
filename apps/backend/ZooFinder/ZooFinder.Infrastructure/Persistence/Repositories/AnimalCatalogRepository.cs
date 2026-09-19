using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Animals.Catalog.Interfaces;
using ZooFinder.Domain.Animals;
using ZooFinder.Infrastructure.Persistence.Pagination;

namespace ZooFinder.Infrastructure.Persistence.Repositories;

public sealed class AnimalCatalogRepository : IAnimalCatalogRepository
{
    private readonly ZooFinderDbContext _dbContext;

    public AnimalCatalogRepository(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<Animal?> GetBySourceItemAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.Animals.AsNoTracking().SingleOrDefaultAsync(animal =>
            animal.InformationSource == informationSource &&
            animal.SourceItemId == sourceItemId &&
            animal.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<CursorPageResponse<Animal>> SearchAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        string scope = DatabaseCursor.GetScope("animals", searchTerm, languageCode);
        var cursor = DatabaseCursor.Parse(pageRequest, scope);
        var query = _dbContext.Animals.AsNoTracking().Where(animal =>
            animal.LanguageCode == languageCode &&
            (animal.Title.Contains(searchTerm) ||
             (animal.ScientificName != null && animal.ScientificName.Contains(searchTerm))));

        if (cursor != null)
        {
            query = query.Where(animal => animal.CreatedAtUtc < cursor.CreatedAtUtc ||
                (animal.CreatedAtUtc == cursor.CreatedAtUtc && animal.Id.CompareTo(cursor.Id) < 0));
        }

        var items = await query.OrderByDescending(animal => animal.CreatedAtUtc)
            .ThenByDescending(animal => animal.Id).Take(pageRequest.Limit + 1)
            .ToListAsync(cancellationToken);
        bool hasMore = items.Count > pageRequest.Limit;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new CursorPageResponse<Animal>(items, hasMore
            ? DatabaseCursor.Encode(scope, items[^1].CreatedAtUtc, items[^1].Id)
            : null);
    }
}
