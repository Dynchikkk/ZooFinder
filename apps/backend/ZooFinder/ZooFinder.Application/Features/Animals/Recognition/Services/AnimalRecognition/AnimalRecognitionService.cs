using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Extensions;
using ZooFinder.Application.Common.Language.Validators;
using ZooFinder.Application.Features.Animals.Recognition.DataSources;
using ZooFinder.Application.Features.Animals.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Animals.Recognition.Services.AnimalRecognition;

public sealed class AnimalRecognitionService : IAnimalRecognitionService
{
    private const long MaximumImageLength = 10 * 1024 * 1024;
    private const int MaximumFileNameLength = 255;
    private readonly IAnimalRecognitionProvider _provider;
    private readonly IAnimalRecognitionDataSource _parks;

    public AnimalRecognitionService(IAnimalRecognitionProvider provider, IAnimalRecognitionDataSource parks)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(parks);
        _provider = provider; _parks = parks;
    }

    public async Task<AnimalRecognitionResponse> RecognizeAsync(
        AnimalRecognitionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string fileName = request.FileName?.Trim() ?? string.Empty;
        string contentType = request.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        string language = request.LanguageCode.NormalizeLanguageCode();
        if (request.ImageStream == null || !request.ImageStream.CanRead)
            throw new RequestValidationException("Image stream must be readable.");
        if (fileName.Length == 0 || fileName.Length > MaximumFileNameLength)
            throw new RequestValidationException("Image file name length is invalid.");
        if (request.Length < 1 || request.Length > MaximumImageLength)
            throw new RequestValidationException("Image length is invalid.");
        LanguageCodeValidator.Validate(language);

        IReadOnlyList<ParkRecognitionAnimalResult> animals = [];
        if (request.ParkId.HasValue)
        {
            if (request.ParkId.Value == Guid.Empty) throw new RequestValidationException("Park ID must not be empty.");
            var park = await _parks.GetParkContextAsync(request.ParkId.Value, cancellationToken)
                ?? throw new NotFoundException("Park was not found.");
            if (park.Status != ParkStatus.Active) throw new ConflictException("Park is suspended.");
            animals = park.Animals;
        }

        var candidates = animals.Select(animal => new AnimalRecognitionContextCandidate(
            animal.AnimalId, animal.CommonName, animal.ScientificName)).ToArray();
        var result = await _provider.RecognizeAsync(new AnimalRecognitionProviderRequest(
            request.ImageStream, contentType, language, candidates), cancellationToken)
            ?? throw new InvalidOperationException("Recognition provider returned no result.");
        ValidateResult(result, candidates);
        var links = animals.ToDictionary(animal => animal.AnimalId, animal => animal.ParkAnimalId);
        var names = candidates.ToDictionary(candidate => candidate.AnimalId);
        return new AnimalRecognitionResponse(result.Status,
            GetCommonName(result.AnimalId, result.CommonName, names),
            GetScientificName(result.AnimalId, result.ScientificName, names),
            result.Alternatives.Select(candidate => new AnimalRecognitionCandidateResponse(
                GetCommonName(candidate.AnimalId, candidate.CommonName, names),
                GetScientificName(candidate.AnimalId, candidate.ScientificName, names),
                GetLink(candidate.AnimalId, links))).ToArray(),
            GetLink(result.AnimalId, links), result.Execution);
    }

    private static void ValidateResult(
        AnimalRecognitionProviderResult result, IReadOnlyList<AnimalRecognitionContextCandidate> context)
    {
        if (!Enum.IsDefined(result.Status) || result.Alternatives == null)
            throw new InvalidOperationException("Recognition provider returned an invalid status or alternatives.");
        var validIds = context.Select(candidate => candidate.AnimalId).ToHashSet();
        if (result.Status == AnimalRecognitionStatus.Recognized &&
            string.IsNullOrWhiteSpace(result.CommonName) && string.IsNullOrWhiteSpace(result.ScientificName) &&
            !result.AnimalId.HasValue)
            throw new InvalidOperationException("Recognized animal has no name or context ID.");
        if (result.AnimalId.HasValue && !validIds.Contains(result.AnimalId.Value))
            throw new InvalidOperationException("Recognition provider returned an ID outside the park context.");
        if (result.Status == AnimalRecognitionStatus.NoAnimal &&
            (result.AnimalId.HasValue || !string.IsNullOrWhiteSpace(result.CommonName) ||
             !string.IsNullOrWhiteSpace(result.ScientificName) || result.Alternatives.Count > 0))
            throw new InvalidOperationException("NoAnimal result contains animal candidates.");
        var seenIds = new HashSet<Guid>();
        if (result.AnimalId.HasValue) seenIds.Add(result.AnimalId.Value);
        foreach (var alternative in result.Alternatives)
        {
            if (alternative == null || (string.IsNullOrWhiteSpace(alternative.CommonName) &&
                string.IsNullOrWhiteSpace(alternative.ScientificName) && !alternative.AnimalId.HasValue))
                throw new InvalidOperationException("Recognition alternative is empty.");
            if (alternative.AnimalId.HasValue &&
                (!validIds.Contains(alternative.AnimalId.Value) || !seenIds.Add(alternative.AnimalId.Value)))
                throw new InvalidOperationException("Recognition alternative contains an invalid or duplicate ID.");
        }
        if (result.Execution != null && (string.IsNullOrWhiteSpace(result.Execution.ModelId) ||
            !double.IsFinite(result.Execution.InferenceMilliseconds) || result.Execution.InferenceMilliseconds < 0))
            throw new InvalidOperationException("Recognition execution metadata is invalid.");
    }

    private static Guid? GetLink(Guid? animalId, IReadOnlyDictionary<Guid, Guid> links) =>
        animalId.HasValue && links.TryGetValue(animalId.Value, out Guid linkId) ? linkId : null;

    private static string? GetCommonName(
        Guid? id, string? name, IReadOnlyDictionary<Guid, AnimalRecognitionContextCandidate> context) =>
        id.HasValue ? Normalize(context[id.Value].CommonName) : Normalize(name);

    private static string? GetScientificName(
        Guid? id, string? name, IReadOnlyDictionary<Guid, AnimalRecognitionContextCandidate> context) =>
        id.HasValue ? Normalize(context[id.Value].ScientificName) : Normalize(name);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
