using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Application.Modules.Parks.Modules.Catalog.Services.Parks;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Api.Controllers.Parks;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class ParksController : ControllerBase
{
    private readonly IParkService _parkService;

    public ParksController(IParkService parkService)
    {
        _parkService = parkService;
    }

    [HttpGet]
    public async Task<ActionResult<OffsetPageResponse<ParkResponse>>> Search(
        [FromQuery] ParkSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _parkService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{parkId:guid}")]
    public async Task<ActionResult<ParkResponse>> Get([FromRoute] Guid parkId, CancellationToken cancellationToken)
    {
        ParkResponse response = await _parkService.GetAsync(parkId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ParkResponse>> Create(
        [FromBody] CreateParkRequest request,
        CancellationToken cancellationToken)
    {
        ParkResponse response = await _parkService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { parkId = response.Id }, response);
    }

    [HttpPut("{parkId:guid}")]
    public async Task<ActionResult<ParkResponse>> Update(
        [FromRoute] Guid parkId,
        [FromBody] UpdateParkHttpRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = new UpdateParkRequest(parkId, request.Name, request.Slug, request.Description, request.Address);
        ParkResponse response = await _parkService.UpdateAsync(serviceRequest, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{parkId:guid}/status")]
    public async Task<ActionResult<ParkResponse>> SetStatus(
        [FromRoute] Guid parkId,
        [FromBody] SetParkStatusRequest request,
        CancellationToken cancellationToken)
    {
        ParkResponse response = await _parkService.SetStatusAsync(parkId, request.Status, cancellationToken);
        return Ok(response);
    }
}
