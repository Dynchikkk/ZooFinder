namespace ZooFinder.Application.Features.Discussions.Messages.Contracts;

public sealed record SendMessageRequest(
    Guid DiscussionRoomId,
    string Content);
