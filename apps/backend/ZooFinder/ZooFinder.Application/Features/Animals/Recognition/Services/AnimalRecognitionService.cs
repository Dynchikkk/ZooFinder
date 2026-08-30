using ZooFinder.Application.Common.Exceptions;
using ZooFinder.Application.Features.Animals.Common.Extensions;
using ZooFinder.Application.Features.Animals.Common.Validators;
using ZooFinder.Application.Features.Animals.Recognition.Contracts;
using ZooFinder.Application.Features.Animals.Recognition.Interfaces;
using ZooFinder.Application.Features.Animals.Recognition.Mappers;

namespace ZooFinder.Application.Features.Animals.Recognition.Services;

public sealed class AnimalRecognitionService : IAnimalRecognitionService
{
    private const long MaximumImageLength = 10 * 1024 * 1024;
    private const int MaximumFileNameLength = 255;

    private readonly IAnimalRecognitionProvider _recognitionProvider;

    public AnimalRecognitionService(IAnimalRecognitionProvider recognitionProvider)
    {
        _recognitionProvider = recognitionProvider;
    }

    public async Task<AnimalRecognitionResponse> RecognizeAsync(
        AnimalRecognitionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string fileName = request.FileName?.Trim() ?? string.Empty;
        string contentType = request.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        Validate(request.ImageStream, fileName, contentType, request.Length, languageCode);

        AnimalRecognitionProviderResult result = await _recognitionProvider.RecognizeAsync(
            request.ImageStream,
            contentType,
            languageCode,
            cancellationToken);

        ValidateProviderResult(result);

        return AnimalRecognitionMapper.ToResponse(result);
    }

    private static void Validate(
        Stream imageStream,
        string fileName,
        string contentType,
        long length,
        string languageCode)
    {
        if (imageStream == null || !imageStream.CanRead)
        {
            throw new RequestValidationException("Image stream must be readable.");
        }

        if (fileName.Length == 0 || fileName.Length > MaximumFileNameLength)
        {
            throw new RequestValidationException(
                $"File name length must be between 1 and {MaximumFileNameLength} characters.");
        }

        // TODO: Validate the actual file type. The declared content type is not verified yet.

        if (length < 1 || length > MaximumImageLength)
        {
            throw new RequestValidationException(
                $"Image length must be between 1 and {MaximumImageLength} bytes.");
        }

        AnimalInformationValidator.ValidateLanguageCode(languageCode);
    }

    private static void ValidateProviderResult(AnimalRecognitionProviderResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsAnimal &&
            string.IsNullOrWhiteSpace(result.CommonName) &&
            string.IsNullOrWhiteSpace(result.ScientificName))
        {
            throw new InvalidOperationException(
                "Animal recognition provider returned an animal without a name.");
        }
    }
}
