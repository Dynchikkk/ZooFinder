using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Api.Controllers.Parks.Import;
using ZooFinder.Application.Modules.Parks.Modules.Import.Services.ParkAnimalImport;

namespace ZooFinder.Api.Controllers.Parks;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class ParkImportController : ControllerBase
{
    private readonly IParkAnimalImportService _parkAnimalImportService;
    private readonly ParkAnimalCsvReader _csvReader;

    public ParkImportController(IParkAnimalImportService parkAnimalImportService, ParkAnimalCsvReader csvReader)
    {
        _parkAnimalImportService = parkAnimalImportService;
        _csvReader = csvReader;
    }

    [HttpPost("{parkId:guid}/preview")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ParkAnimalImportPreviewResponse>> Preview(
        [FromRoute] Guid parkId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var rows = await _csvReader.ReadAsync(file, cancellationToken);
        var request = new PreviewParkAnimalImportRequest(parkId, rows);
        ParkAnimalImportPreviewResponse response = await _parkAnimalImportService.PreviewAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{parkId:guid}/apply")]
    public async Task<ActionResult<ParkAnimalImportResponse>> Apply(
        [FromRoute] Guid parkId,
        [FromBody] ApplyParkAnimalImportHttpRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = new ApplyParkAnimalImportRequest(parkId, request.Rows);
        ParkAnimalImportResponse response = await _parkAnimalImportService.ApplyAsync(serviceRequest, cancellationToken);
        return Ok(response);
    }
}
