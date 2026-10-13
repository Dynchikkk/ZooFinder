using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Domain.Parks;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Parks.DataSources;

public sealed class ParkAnimalDataSource(ZooFinderDbContext dbContext, TimeProvider timeProvider) : IParkAnimalDataSource
{
    public Task<ParkAnimal?> GetAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        return dbContext.ParkAnimals
            .AsNoTracking()
            .Include(link => link.Animal)
            .SingleOrDefaultAsync(link => link.Id == parkAnimalId, cancellationToken);
    }

    public async Task<OffsetPageResponse<ParkAnimal>> SearchAsync(
        Guid parkId,
        bool includeUnpublished,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ParkAnimals
            .AsNoTracking()
            .Where(link => link.ParkId == parkId);
        if (!includeUnpublished)
        {
            query = query.Where(link => link.IsPublished);
        }

        long count = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Include(link => link.Animal)
            .OrderBy(link => link.Animal.Title)
            .ThenBy(link => link.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);
        return new OffsetPageResponse<ParkAnimal>(items, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<ParkAnimal> UpsertAsync(ParkAnimal parkAnimal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkAnimal);

        var links = await UpsertBatchAsync(parkAnimal.ParkId, [parkAnimal], cancellationToken);
        return links[0];
    }

    public async Task<IReadOnlyList<ParkAnimal>> UpsertBatchAsync(
        Guid parkId,
        IReadOnlyList<ParkAnimal> parkAnimals,
        CancellationToken cancellationToken)
    {
        Guid[] animalIds = GetBatchAnimalIds(parkId, parkAnimals);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        if (!await dbContext.Parks.AnyAsync(park => park.Id == parkId, cancellationToken))
        {
            throw new NotFoundException("Park was not found.");
        }

        int animalCount = await dbContext.Animals
            .Where(animal => animalIds.Contains(animal.Id))
            .CountAsync(cancellationToken);

        if (animalCount != animalIds.Length)
        {
            throw new ConflictException("A shared animal is no longer available.");
        }

        var existingIds = await dbContext.ParkAnimals
            .IgnoreQueryFilters()
            .Where(link => link.ParkId == parkId && animalIds.Contains(link.AnimalId))
            .Select(link => new { link.AnimalId, link.Id })
            .ToDictionaryAsync(link => link.AnimalId, link => link.Id, cancellationToken);
        var added = new List<ParkAnimal>();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            foreach (var requested in parkAnimals)
            {
                if (existingIds.TryGetValue(requested.AnimalId, out Guid linkId))
                {
                    await RestoreAndUpdateAsync(linkId, requested, now, cancellationToken);
                }
                else
                {
                    dbContext.Entry(requested).State = EntityState.Added;
                    added.Add(requested);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var saved = await dbContext.ParkAnimals
                .AsNoTracking()
                .Include(link => link.Animal)
                .Where(link => link.ParkId == parkId && animalIds.Contains(link.AnimalId))
                .ToDictionaryAsync(link => link.AnimalId, cancellationToken);
            var result = parkAnimals
                .Select(requested => saved[requested.AnimalId])
                .ToArray();

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            foreach (var link in added)
            {
                dbContext.Entry(link).State = EntityState.Detached;
            }

            throw;
        }
    }

    private static Guid[] GetBatchAnimalIds(Guid parkId, IReadOnlyList<ParkAnimal> parkAnimals)
    {
        ArgumentNullException.ThrowIfNull(parkAnimals);

        var animalIds = new HashSet<Guid>();

        foreach (var link in parkAnimals)
        {
            if (link.ParkId != parkId || !animalIds.Add(link.AnimalId))
            {
                throw new ArgumentException("Park links must have one park and distinct species.", nameof(parkAnimals));
            }
        }

        return animalIds.ToArray();
    }

    private Task<int> RestoreAndUpdateAsync(Guid linkId, ParkAnimal requested, DateTime now, CancellationToken cancellationToken)
    {
        return dbContext.ParkAnimals
            .IgnoreQueryFilters()
            .Where(link => link.Id == linkId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(link => link.LocalDescription, requested.LocalDescription)
                .SetProperty(link => link.IsPublished, requested.IsPublished)
                .SetProperty(link => link.IsDeleted, false)
                .SetProperty(link => link.DeletedAtUtc, (DateTime?)null)
                .SetProperty(link => link.UpdatedAtUtc, now), cancellationToken);
    }

    public async Task UpdateAsync(ParkAnimal parkAnimal, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int count = await VisibleLinksForUpdate()
            .Where(link => link.Id == parkAnimal.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(link => link.LocalDescription, parkAnimal.LocalDescription)
                .SetProperty(link => link.IsPublished, parkAnimal.IsPublished)
                .SetProperty(link => link.UpdatedAtUtc, now), cancellationToken);
        if (count == 0)
        {
            throw new ConflictException("Park animal is no longer available.");
        }
    }

    public async Task RemoveAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int count = await VisibleLinksForUpdate()
            .Where(link => link.Id == parkAnimalId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(link => link.IsDeleted, true)
                .SetProperty(link => link.DeletedAtUtc, now)
                .SetProperty(link => link.UpdatedAtUtc, now), cancellationToken);
        if (count == 0)
        {
            throw new NotFoundException("Park animal was not found.");
        }
    }

    private IQueryable<ParkAnimal> VisibleLinksForUpdate()
    {
        // Explicit subqueries keep the update target to one table. Navigation
        // filters can generate invalid UPDATE aliases on some providers.
        return dbContext.ParkAnimals
            .IgnoreQueryFilters()
            .Where(link => !link.IsDeleted &&
                    dbContext.Parks
                        .Where(park => !park.IsDeleted)
                        .Select(park => park.Id)
                        .Contains(link.ParkId) &&
                    dbContext.Animals
                        .Where(animal => !animal.IsDeleted)
                        .Select(animal => animal.Id)
                        .Contains(link.AnimalId));
    }
}
