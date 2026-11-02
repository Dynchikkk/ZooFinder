using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Api.Authentication;
using ZooFinder.Application.Modules.Discussions.Modules.Messages.Services.DiscussionMessages;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Api.Controllers.Discussions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DiscussionMessagesController : ControllerBase
{
    private readonly IDiscussionMessageService _discussionMessageService;

    public DiscussionMessagesController(IDiscussionMessageService discussionMessageService)
    {
        _discussionMessageService = discussionMessageService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<CursorPageResponse<DiscussionMessageResponse>>> GetHistory(
        [FromQuery] MessageHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _discussionMessageService.GetMessagesAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<DiscussionMessageResponse>> Send(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        DiscussionMessageResponse response = await _discussionMessageService.SendMessageAsync(
            userAccountId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{messageId:guid}")]
    public async Task<ActionResult<DiscussionMessageResponse>> Edit(
        [FromRoute] Guid messageId,
        [FromBody] EditDiscussionMessageHttpRequest request,
        CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        var serviceRequest = new EditMessageRequest(messageId, request.Content);
        DiscussionMessageResponse response = await _discussionMessageService.EditMessageAsync(
            userAccountId, serviceRequest, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid messageId, CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        await _discussionMessageService.DeleteMessageAsync(
            userAccountId, new DeleteMessageRequest(messageId), cancellationToken);
        return NoContent();
    }
}
