using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Api.Authentication;
using ZooFinder.Application.Modules.Discussions.Modules.Rooms.Services.DiscussionRooms;

namespace ZooFinder.Api.Controllers.Discussions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DiscussionRoomsController : ControllerBase
{
    private readonly IDiscussionRoomService _discussionRoomService;

    public DiscussionRoomsController(IDiscussionRoomService discussionRoomService)
    {
        _discussionRoomService = discussionRoomService;
    }

    [HttpGet("{roomId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<DiscussionRoomResponse>> Get([FromRoute] Guid roomId, CancellationToken cancellationToken)
    {
        DiscussionRoomResponse response = await _discussionRoomService.GetRoomAsync(roomId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("general")]
    [AllowAnonymous]
    public async Task<ActionResult<DiscussionRoomResponse>> GetGeneral(
        [FromQuery] Guid animalId,
        CancellationToken cancellationToken)
    {
        DiscussionRoomResponse? response = await _discussionRoomService.GetGeneralRoomAsync(animalId, cancellationToken);
        return response == null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<DiscussionRoomResponse>> Create(
        [FromBody] CreateDiscussionRequest request,
        CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        DiscussionRoomResponse response = await _discussionRoomService.CreateDiscussionAsync(
            userAccountId, request, cancellationToken);
        return Ok(response);
    }
}
