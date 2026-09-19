using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Discussions.Common.Interfaces;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;
using ZooFinder.Infrastructure.Persistence.Pagination;

namespace ZooFinder.Infrastructure.Persistence.Repositories;

public sealed class DiscussionRepository : IDiscussionRepository
{
    private readonly ZooFinderDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DiscussionRepository(
        ZooFinderDbContext dbContext,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<DiscussionRoom?> GetGeneralRoomByAnimalSourceAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms.AsNoTracking().SingleOrDefaultAsync(room =>
            room.Type == DiscussionRoomType.General &&
            room.Animal.InformationSource == informationSource &&
            room.Animal.SourceItemId == sourceItemId &&
            room.Animal.LanguageCode == languageCode, cancellationToken);
    }

    public Task<DiscussionRoom?> GetGeneralRoomByAnimalIdAsync(
        Guid animalId,
        CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms.AsNoTracking().SingleOrDefaultAsync(
            room => room.AnimalId == animalId && room.Type == DiscussionRoomType.General, cancellationToken);
    }

    public Task<DiscussionRoom?> GetRoomByIdAsync(
        Guid discussionRoomId,
        CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms.AsNoTracking()
            .SingleOrDefaultAsync(room => room.Id == discussionRoomId, cancellationToken);
    }

    public async Task<CursorPageResponse<DiscussionMessage>> GetMessagesWithAuthorProfilesAsync(
        Guid discussionRoomId,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        string scope = DatabaseCursor.GetScope("messages", discussionRoomId.ToString("N"));
        var cursor = DatabaseCursor.Parse(pageRequest, scope);
        var query = HistoryQuery().Where(message => message.DiscussionRoomId == discussionRoomId);
        if (cursor != null)
        {
            query = query.Where(message => message.CreatedAtUtc < cursor.CreatedAtUtc ||
                (message.CreatedAtUtc == cursor.CreatedAtUtc && message.Id.CompareTo(cursor.Id) < 0));
        }

        var items = await query.OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id).Take(pageRequest.Limit + 1)
            .ToListAsync(cancellationToken);
        bool hasMore = items.Count > pageRequest.Limit;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new CursorPageResponse<DiscussionMessage>(items, hasMore
            ? DatabaseCursor.Encode(scope, items[^1].CreatedAtUtc, items[^1].Id)
            : null);
    }

    public Task<DiscussionMessage?> GetMessageWithRoomAndAuthorProfileByIdAsync(
        Guid discussionMessageId,
        CancellationToken cancellationToken)
    {
        return HistoryQuery().Include(message => message.DiscussionRoom)
            .SingleOrDefaultAsync(message => message.Id == discussionMessageId, cancellationToken);
    }

    public Task<UserAccount?> GetUserAccountWithProfileByIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return _dbContext.UserAccounts.AsNoTracking().Include(account => account.UserProfile)
            .SingleOrDefaultAsync(account => account.Id == userAccountId, cancellationToken);
    }

    public async Task<DiscussionRoom> GetOrCreateDiscussionAsync(
        Animal animal,
        DiscussionRoom generalRoom,
        CancellationToken cancellationToken)
    {
        var existingRoom = await GetGeneralRoomByAnimalSourceAsync(
            animal.InformationSource, animal.SourceItemId, animal.LanguageCode, cancellationToken);
        if (existingRoom != null)
        {
            return existingRoom;
        }

        _dbContext.Animals.Add(animal);
        _dbContext.Entry(generalRoom).State = EntityState.Added;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return generalRoom;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(generalRoom).State = EntityState.Detached;
            _dbContext.Entry(animal).State = EntityState.Detached;

            existingRoom = await GetGeneralRoomByAnimalSourceAsync(
                animal.InformationSource, animal.SourceItemId, animal.LanguageCode, cancellationToken);
            if (existingRoom != null)
            {
                return existingRoom;
            }

            if (await _dbContext.Animals.IgnoreQueryFilters().AnyAsync(existing =>
                    existing.InformationSource == animal.InformationSource &&
                    existing.SourceItemId == animal.SourceItemId &&
                    existing.LanguageCode == animal.LanguageCode && existing.IsDeleted, cancellationToken))
            {
                throw new ConflictException("The animal has been deleted.");
            }

            throw;
        }
    }

    public async Task AddMessageAsync(
        DiscussionMessage discussionMessage,
        CancellationToken cancellationToken)
    {
        // Only the message is new; room and author were read without tracking.
        _dbContext.Entry(discussionMessage).State = EntityState.Added;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMessageAsync(
        DiscussionMessage discussionMessage,
        CancellationToken cancellationToken)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        var query = _dbContext.DiscussionMessages.Where(message =>
            message.Id == discussionMessage.Id && !message.DiscussionRoom.IsClosed);

        int count;
        if (discussionMessage.IsDeleted)
        {
            count = await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.IsDeleted, true)
                .SetProperty(message => message.DeletedAtUtc, discussionMessage.DeletedAtUtc ?? now)
                .SetProperty(message => message.UpdatedAtUtc, now), cancellationToken);
        }
        else
        {
            count = await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Content, discussionMessage.Content)
                .SetProperty(message => message.EditedAtUtc, discussionMessage.EditedAtUtc)
                .SetProperty(message => message.UpdatedAtUtc, now), cancellationToken);
        }

        if (count == 0)
        {
            throw new ConflictException("The message was deleted or its room is no longer writable.");
        }

        discussionMessage.UpdatedAtUtc = now;
    }

    private IQueryable<DiscussionMessage> HistoryQuery()
    {
        // History retains tombstones and authors even after soft deletion.
        // Reapply room/animal visibility explicitly after disabling global filters.
        return _dbContext.DiscussionMessages.IgnoreQueryFilters().AsNoTracking()
            .Where(message => !message.DiscussionRoom.IsDeleted && !message.DiscussionRoom.Animal.IsDeleted)
            .Include(message => message.AuthorUserAccount).ThenInclude(account => account.UserProfile);
    }
}
