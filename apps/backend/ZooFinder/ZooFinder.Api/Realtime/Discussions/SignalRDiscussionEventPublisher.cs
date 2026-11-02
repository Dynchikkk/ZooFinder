using Microsoft.AspNetCore.SignalR;
using ZooFinder.Application.Modules.Discussions.Modules.Messages.Dependencies.DiscussionEvents;

namespace ZooFinder.Api.Realtime.Discussions;

public sealed class SignalRDiscussionEventPublisher : IDiscussionEventPublisher
{
    private const string MessageCreatedEvent = "MessageCreated";
    private const string MessageUpdatedEvent = "MessageUpdated";
    private const string MessageDeletedEvent = "MessageDeleted";

    private readonly IHubContext<DiscussionHub> _hubContext;

    public SignalRDiscussionEventPublisher(IHubContext<DiscussionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishMessageCreatedAsync(DiscussionMessageEvent message, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(DiscussionHub.GetRoomGroupName(message.DiscussionRoomId))
            .SendAsync(MessageCreatedEvent, message, cancellationToken);
    }

    public Task PublishMessageUpdatedAsync(DiscussionMessageEvent message, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(DiscussionHub.GetRoomGroupName(message.DiscussionRoomId))
            .SendAsync(MessageUpdatedEvent, message, cancellationToken);
    }

    public Task PublishMessageDeletedAsync(
        Guid discussionRoomId,
        Guid discussionMessageId,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken)
    {
        var message = new DiscussionMessageDeletedEvent(discussionRoomId, discussionMessageId, deletedAtUtc);
        return _hubContext.Clients.Group(DiscussionHub.GetRoomGroupName(discussionRoomId))
            .SendAsync(MessageDeletedEvent, message, cancellationToken);
    }
}
