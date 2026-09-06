using ZooFinder.Application.Features.Discussions.Messages.Contracts;

namespace ZooFinder.Application.Features.Discussions.Messages.Interfaces;

public interface IDiscussionEventPublisher
{
    Task PublishMessageCreatedAsync(
        DiscussionMessageResponse message,
        CancellationToken cancellationToken);

    Task PublishMessageUpdatedAsync(
        DiscussionMessageResponse message,
        CancellationToken cancellationToken);

    Task PublishMessageDeletedAsync(
        Guid discussionRoomId,
        Guid discussionMessageId,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken);
}
