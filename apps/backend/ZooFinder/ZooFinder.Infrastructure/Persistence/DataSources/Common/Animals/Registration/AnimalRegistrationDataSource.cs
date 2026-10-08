using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.Animals.Registration.DataSources;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Infrastructure.Persistence.DataSources.Common.Animals.Registration;

public sealed class AnimalRegistrationDataSource : IAnimalRegistrationDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public AnimalRegistrationDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<DiscussionRoom?> GetRegisteredRoomAsync(string source, string itemId,
        string language, CancellationToken cancellationToken) =>
        _dbContext.DiscussionRooms.AsNoTracking().Include(room => room.Animal)
            .SingleOrDefaultAsync(room => room.Type == DiscussionRoomType.General &&
                room.Animal.InformationSource == source && room.Animal.SourceItemId == itemId &&
                room.Animal.LanguageCode == language, cancellationToken);

    public async Task<DiscussionRoom> GetOrCreateAsync(
        Animal animal, DiscussionRoom generalRoom, CancellationToken cancellationToken)
    {
        try
        {
            return await RegisterAsync(animal, generalRoom, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A competing registration may have committed the same source identity.
            var registered = await GetRegisteredRoomAsync(animal.InformationSource,
                animal.SourceItemId, animal.LanguageCode, cancellationToken);
            if (registered != null) return registered;
            throw;
        }
    }

    private async Task<DiscussionRoom> RegisterAsync(Animal animal, DiscussionRoom room, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        bool addedAnimal = false;
        bool addedRoom = false;
        try
        {
            var existing = await FindMatchingAnimalAsync(animal, cancellationToken);
            if (existing != null)
            {
                if (existing.IsDeleted) throw new ConflictException("The shared animal has been deleted.");
                var existingRoom = existing.DiscussionRooms
                    .SingleOrDefault(candidate => candidate.Type == DiscussionRoomType.General);
                if (existingRoom != null)
                {
                    if (existingRoom.IsDeleted) throw new ConflictException("The General room has been deleted.");
                    await transaction.CommitAsync(cancellationToken);
                    return existingRoom;
                }
                room.AnimalId = existing.Id;
                room.Animal = existing;
                _dbContext.Entry(room).State = EntityState.Added;
                addedRoom = true;
            }
            else
            {
                _dbContext.Animals.Add(animal);
                addedAnimal = true;
                addedRoom = true;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return room;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (addedRoom) _dbContext.Entry(room).State = EntityState.Detached;
            if (addedAnimal) _dbContext.Entry(animal).State = EntityState.Detached;
            throw;
        }
    }

    private async Task<Animal?> FindMatchingAnimalAsync(Animal animal, CancellationToken cancellationToken)
    {
        var query = _dbContext.Animals.IgnoreQueryFilters().Include(candidate => candidate.DiscussionRooms)
            .Where(candidate => candidate.LanguageCode == animal.LanguageCode);
        var bySource = await query.SingleOrDefaultAsync(candidate => candidate.InformationSource == animal.InformationSource &&
            candidate.SourceItemId == animal.SourceItemId, cancellationToken);
        if (bySource != null) return bySource;

        string scientific = animal.ScientificName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (scientific.Length > 0)
        {
            var bySpecies = await query.Where(candidate => candidate.ScientificName != null &&
                candidate.ScientificName.Trim().ToLower() == scientific).Take(2).ToListAsync(cancellationToken);
            if (bySpecies.Count > 1) throw new ConflictException("More than one shared card matches the scientific name.");
            if (bySpecies.Count == 1) return bySpecies[0];
        }
        string title = animal.Title.Trim().ToLowerInvariant();
        var byName = await query.Where(candidate => candidate.Title.Trim().ToLower() == title)
            .Take(2).ToListAsync(cancellationToken);
        if (byName.Count > 1) throw new ConflictException("Animal name is ambiguous.");
        if (byName.Count == 0) return null;
        string existingScientific = byName[0].ScientificName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (scientific.Length > 0 && existingScientific.Length > 0 && scientific != existingScientific)
            throw new ConflictException("The animal name belongs to a different species.");
        return byName[0];
    }
}
