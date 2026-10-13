using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.Animals.DataSources;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Animals.DataSources;

public sealed class AnimalRegistrationDataSource : IAnimalRegistrationDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public AnimalRegistrationDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<DiscussionRoom?> GetRegisteredRoomAsync(
        string source,
        string itemId,
        string language,
        CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms
            .AsNoTracking()
            .Include(room => room.Animal)
            .SingleOrDefaultAsync(room => room.Type == DiscussionRoomType.General &&
                room.Animal.InformationSource == source && room.Animal.SourceItemId == itemId &&
                room.Animal.LanguageCode == language, cancellationToken);
    }

    public async Task<DiscussionRoom> GetOrCreateAsync(
        Animal animal,
        DiscussionRoom generalRoom,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(animal);
        ArgumentNullException.ThrowIfNull(generalRoom);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        Animal? existingAnimal = await _dbContext.Animals
            .IgnoreQueryFilters()
            .Include(candidate => candidate.DiscussionRooms)
            .SingleOrDefaultAsync(candidate => candidate.InformationSource == animal.InformationSource &&
                candidate.SourceItemId == animal.SourceItemId && candidate.LanguageCode == animal.LanguageCode,
                cancellationToken);

        if (existingAnimal != null)
        {
            if (existingAnimal.IsDeleted)
            {
                throw new ConflictException("The shared animal has been deleted.");
            }

            DiscussionRoom? existingRoom = existingAnimal.DiscussionRooms
                .SingleOrDefault(room => room.Type == DiscussionRoomType.General);

            if (existingRoom != null)
            {
                if (existingRoom.IsDeleted)
                {
                    throw new ConflictException("The General room has been deleted.");
                }

                await transaction.CommitAsync(cancellationToken);
                return existingRoom;
            }

            generalRoom.AnimalId = existingAnimal.Id;
            generalRoom.Animal = existingAnimal;
            _dbContext.Entry(generalRoom).State = EntityState.Added;
        }
        else
        {
            _dbContext.Animals.Add(animal);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return generalRoom;
    }
}
