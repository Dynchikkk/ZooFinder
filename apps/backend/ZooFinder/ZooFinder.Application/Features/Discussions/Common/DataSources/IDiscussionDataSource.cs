using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Discussions.Common.DataSources;

public interface IDiscussionDataSource
{
    Task<DiscussionRoom?> GetGeneralRoomByAnimalSourceAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken);

    Task<DiscussionRoom?> GetGeneralRoomByAnimalIdAsync(
        Guid animalId,
        CancellationToken cancellationToken);

    Task<DiscussionRoom?> GetRoomByIdAsync(
        Guid discussionRoomId,
        CancellationToken cancellationToken);

    Task<CursorPageResponse<DiscussionMessage>> GetMessagesWithAuthorProfilesAsync(
        Guid discussionRoomId,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken);

    Task<DiscussionMessage?> GetMessageWithRoomAndAuthorProfileByIdAsync(
        Guid discussionMessageId,
        CancellationToken cancellationToken);

    Task<UserAccount?> GetUserAccountWithProfileByIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);


    Task AddMessageAsync(
        DiscussionMessage discussionMessage,
        CancellationToken cancellationToken);

    Task UpdateMessageAsync(
        DiscussionMessage discussionMessage,
        CancellationToken cancellationToken);
}
