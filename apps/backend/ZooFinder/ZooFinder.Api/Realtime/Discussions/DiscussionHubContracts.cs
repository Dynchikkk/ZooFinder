namespace ZooFinder.Api.Realtime.Discussions;

public sealed record DiscussionMessageDeletedEvent(
    Guid DiscussionRoomId,
    Guid DiscussionMessageId,
    DateTime DeletedAtUtc);
