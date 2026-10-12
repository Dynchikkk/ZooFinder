namespace ZooFinder.Application.Modules.Discussions.Modules.Messages.Dependencies.DiscussionEvents;

public interface IDiscussionEventPublisher
{
    Task PublishMessageCreatedAsync(
        DiscussionMessageEvent message,
        CancellationToken cancellationToken);

    Task PublishMessageUpdatedAsync(
        DiscussionMessageEvent message,
        CancellationToken cancellationToken);

    Task PublishMessageDeletedAsync(
        Guid discussionRoomId,
        Guid discussionMessageId,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken);
}
