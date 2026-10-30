using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Application.Modules.Animals.Modules.Catalog.Services.AnimalCatalog;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Api.Controllers.Animals;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AnimalCatalogController : ControllerBase
{
    private readonly IAnimalCatalogService _animalCatalogService;

    public AnimalCatalogController(IAnimalCatalogService animalCatalogService)
    {
        _animalCatalogService = animalCatalogService;
    }

    [HttpGet]
    public async Task<ActionResult<CursorPageResponse<AnimalCardResponse>>> Search(
        [FromQuery] AnimalSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _animalCatalogService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("details")]
    public async Task<ActionResult<AnimalPageResponse>> Get(
        [FromQuery] AnimalPageRequest request,
        CancellationToken cancellationToken)
    {
        AnimalPageResponse response = await _animalCatalogService.GetAnimalAsync(request, cancellationToken);
        return Ok(response);
    }
}
