using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Parks.DataSources;

public sealed class ParkAnimalDataSource : IParkAnimalDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public ParkAnimalDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<ParkAnimal?> GetAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        return _dbContext.ParkAnimals
            .AsNoTracking()
            .Include(parkAnimal => parkAnimal.Animal)
            .SingleOrDefaultAsync(parkAnimal => parkAnimal.Id == parkAnimalId, cancellationToken);
    }

    public async Task<OffsetPageResponse<ParkAnimal>> SearchAsync(
        Guid parkId,
        bool includeUnpublished,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ParkAnimals
            .AsNoTracking()
            .Where(parkAnimal => parkAnimal.ParkId == parkId);

        if (!includeUnpublished)
        {
            query = query.Where(parkAnimal => parkAnimal.IsPublished);
        }

        long count = await query.LongCountAsync(cancellationToken);
        var parkAnimals = await query
            .Include(parkAnimal => parkAnimal.Animal)
            .OrderBy(parkAnimal => parkAnimal.Animal.Title)
            .ThenBy(parkAnimal => parkAnimal.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new OffsetPageResponse<ParkAnimal>(parkAnimals, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<ParkAnimal> UpsertAsync(ParkAnimal parkAnimal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkAnimal);

        var parkAnimals = await UpsertBatchAsync(parkAnimal.ParkId, [parkAnimal], cancellationToken);
        return parkAnimals[0];
    }

    public async Task<IReadOnlyList<ParkAnimal>> UpsertBatchAsync(
        Guid parkId,
        IReadOnlyList<ParkAnimal> parkAnimals,
        CancellationToken cancellationToken)
    {
        Guid[] animalIds = GetBatchAnimalIds(parkId, parkAnimals);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        if (!await _dbContext.Parks.AnyAsync(park => park.Id == parkId, cancellationToken))
        {
            throw new NotFoundException("Park was not found.");
        }

        var animals = await _dbContext.Animals
            .Where(animal => animalIds.Contains(animal.Id))
            .ToDictionaryAsync(animal => animal.Id, cancellationToken);

        if (animals.Count != animalIds.Length)
        {
            throw new ConflictException("A shared animal is no longer available.");
        }

        var existingParkAnimals = await _dbContext.ParkAnimals
            .IgnoreQueryFilters()
            .Where(parkAnimal => parkAnimal.ParkId == parkId && animalIds.Contains(parkAnimal.AnimalId))
            .ToDictionaryAsync(parkAnimal => parkAnimal.AnimalId, cancellationToken);
        var savedParkAnimals = new List<ParkAnimal>(parkAnimals.Count);

        try
        {
            foreach (ParkAnimal requestedParkAnimal in parkAnimals)
            {
                if (existingParkAnimals.TryGetValue(requestedParkAnimal.AnimalId, out ParkAnimal? parkAnimal))
                {
                    if (parkAnimal.IsDeleted)
                    {
                        _dbContext.Restore(parkAnimal);
                    }

                    parkAnimal.LocalDescription = requestedParkAnimal.LocalDescription;
                    parkAnimal.IsPublished = requestedParkAnimal.IsPublished;
                }
                else
                {
                    parkAnimal = new ParkAnimal
                    {
                        Id = requestedParkAnimal.Id,
                        ParkId = parkId,
                        AnimalId = requestedParkAnimal.AnimalId,
                        LocalDescription = requestedParkAnimal.LocalDescription,
                        IsPublished = requestedParkAnimal.IsPublished
                    };

                    _dbContext.Entry(parkAnimal).State = EntityState.Added;
                }

                parkAnimal.Animal = animals[parkAnimal.AnimalId];
                savedParkAnimals.Add(parkAnimal);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return savedParkAnimals;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Park animals changed while the batch was being saved.");
        }
    }

    public async Task UpdateAsync(ParkAnimal parkAnimal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkAnimal);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        ParkAnimal? existingParkAnimal = await _dbContext.ParkAnimals
            .SingleOrDefaultAsync(existing => existing.Id == parkAnimal.Id, cancellationToken);

        if (existingParkAnimal == null)
        {
            throw new ConflictException("Park animal is no longer available.");
        }

        existingParkAnimal.LocalDescription = parkAnimal.LocalDescription;
        existingParkAnimal.IsPublished = parkAnimal.IsPublished;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            parkAnimal.UpdatedAtUtc = existingParkAnimal.UpdatedAtUtc;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Park animal is no longer available.");
        }
    }

    public async Task RemoveAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        ParkAnimal? parkAnimal = await _dbContext.ParkAnimals
            .SingleOrDefaultAsync(existing => existing.Id == parkAnimalId, cancellationToken);

        if (parkAnimal == null)
        {
            throw new NotFoundException("Park animal was not found.");
        }

        _dbContext.ParkAnimals.Remove(parkAnimal);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new NotFoundException("Park animal was not found.");
        }
    }

    private static Guid[] GetBatchAnimalIds(Guid parkId, IReadOnlyList<ParkAnimal> parkAnimals)
    {
        ArgumentNullException.ThrowIfNull(parkAnimals);

        var animalIds = new HashSet<Guid>();

        foreach (ParkAnimal parkAnimal in parkAnimals)
        {
            if (parkAnimal.ParkId != parkId || !animalIds.Add(parkAnimal.AnimalId))
            {
                throw new ArgumentException("Park links must have one park and distinct species.", nameof(parkAnimals));
            }
        }

        return animalIds.ToArray();
    }
}
