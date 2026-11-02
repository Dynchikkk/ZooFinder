using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Api.Controllers.Animals.Recognition;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Services.AnimalRecognition;
using ZooFinder.Application.Shared.Language.Constants;

namespace ZooFinder.Api.Controllers.Animals;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AnimalRecognitionController : ControllerBase
{
    private readonly IAnimalRecognitionService _animalRecognitionService;

    public AnimalRecognitionController(IAnimalRecognitionService animalRecognitionService)
    {
        _animalRecognitionService = animalRecognitionService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AnimalRecognitionResponse>> Recognize(
        [FromForm] IFormFile image,
        CancellationToken cancellationToken,
        [FromForm] string languageCode = LanguageCodes.English,
        [FromForm] Guid? parkId = null)
    {
        string contentType = await AnimalRecognitionImageValidator.ValidateAsync(image, cancellationToken);
        await using Stream imageStream = image.OpenReadStream();
        var request = new AnimalRecognitionRequest(imageStream, image.FileName, contentType, image.Length, languageCode, parkId);
        AnimalRecognitionResponse response = await _animalRecognitionService.RecognizeAsync(request, cancellationToken);
        return Ok(response);
    }
}
