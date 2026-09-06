using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Discussions.Common.Interfaces;

public interface IDiscussionRepository
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

    Task AddDiscussionAsync(
        Animal animal,
        DiscussionRoom generalRoom,
        CancellationToken cancellationToken);

    Task AddMessageAsync(
        DiscussionMessage discussionMessage,
        CancellationToken cancellationToken);

    Task UpdateMessageAsync(
        DiscussionMessage discussionMessage,
        CancellationToken cancellationToken);
}
