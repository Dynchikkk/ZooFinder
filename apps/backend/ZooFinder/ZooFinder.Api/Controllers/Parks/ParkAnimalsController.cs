using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Application.Modules.Parks.Modules.Animals.Services.ParkAnimals;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Api.Controllers.Parks;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class ParkAnimalsController : ControllerBase
{
    private readonly IParkAnimalService _parkAnimalService;

    public ParkAnimalsController(IParkAnimalService parkAnimalService)
    {
        _parkAnimalService = parkAnimalService;
    }

    [HttpGet]
    public async Task<ActionResult<OffsetPageResponse<ParkAnimalResponse>>> Search(
        [FromQuery] ParkAnimalSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _parkAnimalService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{parkAnimalId:guid}")]
    public async Task<ActionResult<ParkAnimalResponse>> Get([FromRoute] Guid parkAnimalId, CancellationToken cancellationToken)
    {
        ParkAnimalResponse response = await _parkAnimalService.GetAsync(parkAnimalId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ParkAnimalResponse>> Add(
        [FromBody] AddParkAnimalRequest request,
        CancellationToken cancellationToken)
    {
        ParkAnimalResponse response = await _parkAnimalService.AddAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{parkAnimalId:guid}")]
    public async Task<ActionResult<ParkAnimalResponse>> Update(
        [FromRoute] Guid parkAnimalId,
        [FromBody] UpdateParkAnimalHttpRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = new UpdateParkAnimalRequest(parkAnimalId, request.LocalDescription, request.IsPublished);
        ParkAnimalResponse response = await _parkAnimalService.UpdateAsync(serviceRequest, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{parkAnimalId:guid}")]
    public async Task<IActionResult> Remove([FromRoute] Guid parkAnimalId, CancellationToken cancellationToken)
    {
        await _parkAnimalService.RemoveAsync(parkAnimalId, cancellationToken);
        return NoContent();
    }
}
