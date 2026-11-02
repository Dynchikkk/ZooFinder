using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ZooFinder.Application.Modules.Discussions.Modules.Rooms.Services.DiscussionRooms;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Api.Realtime.Discussions;

[AllowAnonymous]
public sealed class DiscussionHub : Hub
{
    public const string Route = "/hubs/discussions";

    private readonly IDiscussionRoomService _discussionRoomService;

    public DiscussionHub(IDiscussionRoomService discussionRoomService)
    {
        _discussionRoomService = discussionRoomService;
    }

    public async Task JoinRoom(Guid roomId)
    {
        try
        {
            await _discussionRoomService.GetRoomAsync(roomId, Context.ConnectionAborted);
        }
        catch (RequestValidationException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (NotFoundException exception)
        {
            throw new HubException(exception.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId), Context.ConnectionAborted);
    }

    public Task LeaveRoom(Guid roomId)
    {
        if (roomId == Guid.Empty)
        {
            throw new HubException("Discussion room ID must not be empty.");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId), Context.ConnectionAborted);
    }

    internal static string GetRoomGroupName(Guid roomId)
    {
        return $"discussion-room:{roomId:N}";
    }
}
