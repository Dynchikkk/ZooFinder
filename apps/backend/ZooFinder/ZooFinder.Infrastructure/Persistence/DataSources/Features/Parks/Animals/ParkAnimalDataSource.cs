using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Animals.DataSources;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Infrastructure.Persistence.DataSources.Features.Parks.Animals;

public sealed class ParkAnimalDataSource(ZooFinderDbContext dbContext, TimeProvider timeProvider) : IParkAnimalDataSource
{
    public Task<ParkAnimal?> GetAsync(Guid parkAnimalId, CancellationToken cancellationToken) =>
        dbContext.ParkAnimals.AsNoTracking().Include(link => link.Animal)
            .SingleOrDefaultAsync(link => link.Id == parkAnimalId, cancellationToken);

    public async Task<OffsetPageResponse<ParkAnimal>> SearchAsync(Guid parkId, bool includeUnpublished,
        OffsetPageRequest pageRequest, CancellationToken cancellationToken)
    {
        var query = dbContext.ParkAnimals.AsNoTracking().Where(link => link.ParkId == parkId);
        if (!includeUnpublished) query = query.Where(link => link.IsPublished);
        long count = await query.LongCountAsync(cancellationToken);
        var items = await query.Include(link => link.Animal).OrderBy(link => link.Animal.Title).ThenBy(link => link.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize).Take(pageRequest.PageSize).ToListAsync(cancellationToken);
        return new OffsetPageResponse<ParkAnimal>(items, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<ParkAnimal> UpsertAsync(ParkAnimal parkAnimal, CancellationToken cancellationToken) =>
        (await UpsertBatchAsync(parkAnimal.ParkId, [parkAnimal], cancellationToken))[0];

    public async Task<IReadOnlyList<ParkAnimal>> UpsertBatchAsync(Guid parkId,
        IReadOnlyList<ParkAnimal> parkAnimals, CancellationToken cancellationToken)
    {
        if (parkAnimals.Any(link => link.ParkId != parkId) ||
            parkAnimals.Select(link => link.AnimalId).Distinct().Count() != parkAnimals.Count)
            throw new ArgumentException("Park links must have one park and distinct species.", nameof(parkAnimals));
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        if (!await dbContext.Parks.AnyAsync(park => park.Id == parkId, cancellationToken))
            throw new NotFoundException("Park was not found.");
        var animalIds = parkAnimals.Select(link => link.AnimalId).ToArray();
        // Use tracked canonical instances: registration may already have tracked
        // these cards in the same scope, and detached copies would conflict.
        var shared = await dbContext.Animals.Where(animal => animalIds.Contains(animal.Id))
            .ToDictionaryAsync(animal => animal.Id, cancellationToken);
        if (shared.Count != parkAnimals.Count) throw new ConflictException("A shared animal is no longer available.");
        var existing = await dbContext.ParkAnimals.IgnoreQueryFilters().AsNoTracking()
            .Where(link => link.ParkId == parkId && animalIds.Contains(link.AnimalId))
            .ToDictionaryAsync(link => link.AnimalId, cancellationToken);
        var result = new List<ParkAnimal>(parkAnimals.Count);
        var added = new List<ParkAnimal>();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            foreach (var requested in parkAnimals)
            {
                if (existing.TryGetValue(requested.AnimalId, out var link))
                {
                    // Re-adding restores the association, without replacing the shared card or chat.
                    await dbContext.ParkAnimals.IgnoreQueryFilters().Where(candidate => candidate.Id == link.Id)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(candidate => candidate.LocalDescription, requested.LocalDescription)
                            .SetProperty(candidate => candidate.IsPublished, requested.IsPublished)
                            .SetProperty(candidate => candidate.IsDeleted, false)
                            .SetProperty(candidate => candidate.DeletedAtUtc, (DateTime?)null)
                            .SetProperty(candidate => candidate.UpdatedAtUtc, now), cancellationToken);
                    link.LocalDescription = requested.LocalDescription;
                    link.IsPublished = requested.IsPublished;
                    link.IsDeleted = false;
                    link.DeletedAtUtc = null;
                    link.UpdatedAtUtc = now;
                }
                else
                {
                    link = requested;
                    // Only the link is new. Navigations may reference detached, existing entities.
                    dbContext.Entry(link).State = EntityState.Added;
                    added.Add(link);
                }
                link.Animal = shared[link.AnimalId];
                result.Add(link);
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            foreach (var link in added) dbContext.Entry(link).State = EntityState.Detached;
            throw;
        }
    }

    public async Task UpdateAsync(ParkAnimal parkAnimal, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int count = await VisibleLinksForUpdate().Where(link => link.Id == parkAnimal.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(link => link.LocalDescription, parkAnimal.LocalDescription)
            .SetProperty(link => link.IsPublished, parkAnimal.IsPublished)
            .SetProperty(link => link.UpdatedAtUtc, now), cancellationToken);
        if (count == 0) throw new ConflictException("Park animal is no longer available.");
    }

    public async Task RemoveAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int count = await VisibleLinksForUpdate().Where(link => link.Id == parkAnimalId).ExecuteUpdateAsync(setters => setters
            .SetProperty(link => link.IsDeleted, true)
            .SetProperty(link => link.DeletedAtUtc, now)
            .SetProperty(link => link.UpdatedAtUtc, now), cancellationToken);
        if (count == 0) throw new NotFoundException("Park animal was not found.");
    }

    private IQueryable<ParkAnimal> VisibleLinksForUpdate() =>
        // Explicit subqueries keep the update target to one table. Navigation
        // filters can generate invalid UPDATE aliases on some providers.
        dbContext.ParkAnimals.IgnoreQueryFilters().Where(link => !link.IsDeleted &&
            dbContext.Parks.Where(park => !park.IsDeleted).Select(park => park.Id).Contains(link.ParkId) &&
            dbContext.Animals.Where(animal => !animal.IsDeleted).Select(animal => animal.Id).Contains(link.AnimalId));
}
