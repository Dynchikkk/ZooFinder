using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Extensions;
using ZooFinder.Application.Features.Animals.Recognition.DataSources;
using ZooFinder.Application.Features.Animals.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Features.Animals.Recognition.Validators;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Animals.Recognition.Services.AnimalRecognition;

public sealed class AnimalRecognitionService : IAnimalRecognitionService
{
    private readonly IAnimalRecognitionProvider _animalRecognitionProvider;
    private readonly IAnimalRecognitionDataSource _animalRecognitionDataSource;

    public AnimalRecognitionService(
        IAnimalRecognitionProvider animalRecognitionProvider,
        IAnimalRecognitionDataSource animalRecognitionDataSource)
    {
        ArgumentNullException.ThrowIfNull(animalRecognitionProvider);
        ArgumentNullException.ThrowIfNull(animalRecognitionDataSource);

        _animalRecognitionProvider = animalRecognitionProvider;
        _animalRecognitionDataSource = animalRecognitionDataSource;
    }

    public async Task<AnimalRecognitionResponse> RecognizeAsync(
        AnimalRecognitionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string fileName = request.FileName?.Trim() ?? string.Empty;
        string contentType = request.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        AnimalRecognitionValidator.ValidateRequest(request.ImageStream, fileName, request.Length, languageCode);

        var animals = await GetParkAnimalsAsync(request.ParkId, cancellationToken);
        var candidates = animals
            .Select(animal => new AnimalRecognitionContextCandidate(animal.AnimalId, animal.CommonName, animal.ScientificName))
            .ToArray();
        var providerRequest = new AnimalRecognitionProviderRequest(request.ImageStream, contentType, languageCode, candidates);

        var result = await _animalRecognitionProvider.RecognizeAsync(providerRequest, cancellationToken)
            ?? throw new InvalidOperationException("Recognition provider returned no result.");

        AnimalRecognitionValidator.ValidateResult(result, candidates);

        return CreateResponse(result, animals);
    }

    private async Task<IReadOnlyList<ParkRecognitionAnimalResult>> GetParkAnimalsAsync(
        Guid? parkId,
        CancellationToken cancellationToken)
    {
        if (!parkId.HasValue)
        {
            return [];
        }

        AnimalRecognitionValidator.ValidateParkId(parkId.Value);

        var park = await _animalRecognitionDataSource.GetParkContextAsync(parkId.Value, cancellationToken)
            ?? throw new NotFoundException("Park was not found.");

        if (park.Status != ParkStatus.Active)
        {
            throw new ConflictException("Park is suspended.");
        }

        return park.Animals;
    }

    private static AnimalRecognitionResponse CreateResponse(
        AnimalRecognitionProviderResult result,
        IReadOnlyList<ParkRecognitionAnimalResult> animals)
    {
        var animalsById = animals.ToDictionary(animal => animal.AnimalId);
        var recognizedAnimal = CreateCandidateResponse(result.AnimalId, result.CommonName, result.ScientificName, animalsById);
        var alternatives = result.Alternatives
            .Select(candidate => CreateCandidateResponse(
                candidate.AnimalId, candidate.CommonName, candidate.ScientificName, animalsById))
            .ToArray();

        return new AnimalRecognitionResponse(
            result.Status,
            recognizedAnimal.CommonName,
            recognizedAnimal.ScientificName,
            alternatives,
            recognizedAnimal.ParkAnimalId,
            result.Execution);
    }

    private static AnimalRecognitionCandidateResponse CreateCandidateResponse(
        Guid? animalId,
        string? commonName,
        string? scientificName,
        IReadOnlyDictionary<Guid, ParkRecognitionAnimalResult> animalsById)
    {
        if (animalId.HasValue)
        {
            var animal = animalsById[animalId.Value];

            return new AnimalRecognitionCandidateResponse(
                NormalizeName(animal.CommonName), NormalizeName(animal.ScientificName), animal.ParkAnimalId);
        }

        return new AnimalRecognitionCandidateResponse(NormalizeName(commonName), NormalizeName(scientificName));
    }

    private static string? NormalizeName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
