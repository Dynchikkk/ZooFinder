namespace ZooFinder.Application.Features.Discussions.Messages.Contracts;

public sealed record EditMessageRequest(
    Guid DiscussionMessageId,
    string Content);
