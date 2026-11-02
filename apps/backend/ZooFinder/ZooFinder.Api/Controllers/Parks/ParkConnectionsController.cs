using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Application.Modules.Parks.Modules.Connections.Services.ParkConnections;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Api.Controllers.Parks;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class ParkConnectionsController : ControllerBase
{
    private readonly IParkConnectionService _parkConnectionService;

    public ParkConnectionsController(IParkConnectionService parkConnectionService)
    {
        _parkConnectionService = parkConnectionService;
    }

    [HttpGet]
    public async Task<ActionResult<OffsetPageResponse<ParkConnectionResponse>>> Search(
        [FromQuery] Guid parkId,
        [FromQuery] OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var response = await _parkConnectionService.SearchAsync(parkId, pageRequest, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<ParkConnectionResponse>> Get([FromRoute] Guid requestId, CancellationToken cancellationToken)
    {
        ParkConnectionResponse response = await _parkConnectionService.GetAsync(requestId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ParkConnectionResponse>> Create(
        [FromBody] CreateParkConnectionRequest request,
        CancellationToken cancellationToken)
    {
        ParkConnectionResponse response = await _parkConnectionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { requestId = response.Id }, response);
    }

    [HttpPost("{requestId:guid}/approve")]
    public async Task<ActionResult<ParkConnectionResponse>> Approve(
        [FromRoute] Guid requestId,
        [FromBody] ApproveParkConnectionHttpRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = new ApproveParkConnectionRequest(requestId, request.Amount, request.Currency, request.Comment);
        ParkConnectionResponse response = await _parkConnectionService.ApproveAsync(serviceRequest, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{requestId:guid}/confirm-payment")]
    public async Task<ActionResult<ParkConnectionResponse>> ConfirmPayment(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        ParkConnectionResponse response = await _parkConnectionService.ConfirmPaymentAsync(requestId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{requestId:guid}/close")]
    public async Task<ActionResult<ParkConnectionResponse>> Close(
        [FromRoute] Guid requestId,
        [FromBody] CloseParkConnectionHttpRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = new CloseParkConnectionRequest(requestId, request.Status, request.Comment);
        ParkConnectionResponse response = await _parkConnectionService.CloseAsync(serviceRequest, cancellationToken);
        return Ok(response);
    }
}
