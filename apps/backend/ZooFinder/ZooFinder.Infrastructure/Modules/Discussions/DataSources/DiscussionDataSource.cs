using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Discussions.DataSources;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;
using ZooFinder.Infrastructure.Persistence;
using ZooFinder.Infrastructure.Persistence.Pagination;

namespace ZooFinder.Infrastructure.Modules.Discussions.DataSources;

public sealed class DiscussionDataSource : IDiscussionDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public DiscussionDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<DiscussionRoom?> GetGeneralRoomByAnimalSourceAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(room =>
                room.Type == DiscussionRoomType.General && room.Animal.InformationSource == informationSource &&
                room.Animal.SourceItemId == sourceItemId &&
                room.Animal.LanguageCode == languageCode, cancellationToken);
    }

    public Task<DiscussionRoom?> GetGeneralRoomByAnimalIdAsync(Guid animalId, CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(
            room => room.AnimalId == animalId && room.Type == DiscussionRoomType.General, cancellationToken);
    }

    public Task<DiscussionRoom?> GetRoomByIdAsync(Guid discussionRoomId, CancellationToken cancellationToken)
    {
        return _dbContext.DiscussionRooms
            .AsNoTracking()
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

        var items = await query
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(pageRequest.Limit + 1)
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
        return HistoryQuery()
            .Include(message => message.DiscussionRoom)
            .SingleOrDefaultAsync(message => message.Id == discussionMessageId, cancellationToken);
    }

    public Task<UserAccount?> GetUserAccountWithProfileByIdAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        return _dbContext.UserAccounts
            .AsNoTracking()
            .Include(account => account.UserProfile)
            .SingleOrDefaultAsync(account => account.Id == userAccountId, cancellationToken);
    }

    public async Task AddMessageAsync(DiscussionMessage discussionMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discussionMessage);

        // Only the message is new; room and author were read without tracking.
        _dbContext.Entry(discussionMessage).State = EntityState.Added;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMessageAsync(DiscussionMessage discussionMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discussionMessage);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        DiscussionMessage? existingMessage = await _dbContext.DiscussionMessages
            .SingleOrDefaultAsync(message => message.Id == discussionMessage.Id && !message.DiscussionRoom.IsClosed,
                cancellationToken);

        if (existingMessage == null)
        {
            throw new ConflictException("The message was deleted or its room is no longer writable.");
        }

        if (discussionMessage.IsDeleted)
        {
            _dbContext.DiscussionMessages.Remove(existingMessage);
        }
        else
        {
            existingMessage.Content = discussionMessage.Content;
            existingMessage.EditedAtUtc = discussionMessage.EditedAtUtc;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            discussionMessage.UpdatedAtUtc = existingMessage.UpdatedAtUtc;
            discussionMessage.DeletedAtUtc = existingMessage.DeletedAtUtc;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The message was deleted or its room is no longer writable.");
        }
    }

    private IQueryable<DiscussionMessage> HistoryQuery()
    {
        // History retains tombstones and authors even after soft deletion.
        // Reapply room/animal visibility explicitly after disabling global filters.
        return _dbContext.DiscussionMessages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(message => !message.DiscussionRoom.IsDeleted && !message.DiscussionRoom.Animal.IsDeleted)
            .Include(message => message.AuthorUserAccount)
            .ThenInclude(account => account.UserProfile);
    }
}
