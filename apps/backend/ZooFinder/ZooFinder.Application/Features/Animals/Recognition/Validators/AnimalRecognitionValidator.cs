using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Validators;
using ZooFinder.Application.Features.Animals.Recognition.Constants;
using ZooFinder.Application.Features.Animals.Recognition.Dependencies.AnimalRecognition;

namespace ZooFinder.Application.Features.Animals.Recognition.Validators;

public static class AnimalRecognitionValidator
{
    public static void ValidateRequest(Stream? imageStream, string fileName, long length, string languageCode)
    {
        if (imageStream == null || !imageStream.CanRead)
        {
            throw new RequestValidationException("Image stream must be readable.");
        }

        if (fileName.Length == 0 || fileName.Length > AnimalRecognitionConstraints.MaximumFileNameLength)
        {
            throw new RequestValidationException("Image file name length is invalid.");
        }

        if (length < 1 || length > AnimalRecognitionConstraints.MaximumImageLength)
        {
            throw new RequestValidationException("Image length is invalid.");
        }

        LanguageCodeValidator.Validate(languageCode);
    }

    public static void ValidateParkId(Guid parkId)
    {
        if (parkId == Guid.Empty)
        {
            throw new RequestValidationException("Park ID must not be empty.");
        }
    }

    public static void ValidateResult(
        AnimalRecognitionProviderResult result,
        IReadOnlyList<AnimalRecognitionContextCandidate> context)
    {
        if (!Enum.IsDefined(result.Status) || result.Alternatives == null)
        {
            throw new InvalidOperationException("Recognition provider returned an invalid status or alternatives.");
        }

        var validAnimalIds = context
            .Select(candidate => candidate.AnimalId)
            .ToHashSet();

        ValidateMainCandidate(result, validAnimalIds);
        ValidateAlternatives(result.Alternatives, result.AnimalId, validAnimalIds);
        ValidateExecution(result.Execution);
    }

    private static void ValidateMainCandidate(AnimalRecognitionProviderResult result, IReadOnlySet<Guid> validAnimalIds)
    {
        bool hasAnimalIdentity = HasAnimalIdentity(result.AnimalId, result.CommonName, result.ScientificName);

        if (result.Status == AnimalRecognitionStatus.Recognized && !hasAnimalIdentity)
        {
            throw new InvalidOperationException("Recognized animal has no name or context ID.");
        }

        if (result.AnimalId.HasValue && !validAnimalIds.Contains(result.AnimalId.Value))
        {
            throw new InvalidOperationException("Recognition provider returned an ID outside the park context.");
        }

        if (result.Status == AnimalRecognitionStatus.NoAnimal && (hasAnimalIdentity || result.Alternatives.Count > 0))
        {
            throw new InvalidOperationException("NoAnimal result contains animal candidates.");
        }
    }

    private static void ValidateAlternatives(
        IReadOnlyList<AnimalRecognitionProviderCandidate> alternatives,
        Guid? recognizedAnimalId,
        IReadOnlySet<Guid> validAnimalIds)
    {
        var seenAnimalIds = new HashSet<Guid>();

        if (recognizedAnimalId.HasValue)
        {
            seenAnimalIds.Add(recognizedAnimalId.Value);
        }

        foreach (var alternative in alternatives)
        {
            if (alternative == null ||
                !HasAnimalIdentity(alternative.AnimalId, alternative.CommonName, alternative.ScientificName))
            {
                throw new InvalidOperationException("Recognition alternative is empty.");
            }

            if (alternative.AnimalId.HasValue &&
                (!validAnimalIds.Contains(alternative.AnimalId.Value) || !seenAnimalIds.Add(alternative.AnimalId.Value)))
            {
                throw new InvalidOperationException("Recognition alternative contains an invalid or duplicate ID.");
            }
        }
    }

    private static void ValidateExecution(AnimalRecognitionExecutionResult? execution)
    {
        if (execution == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(execution.ModelId) || !double.IsFinite(execution.InferenceMilliseconds) ||
            execution.InferenceMilliseconds < 0)
        {
            throw new InvalidOperationException("Recognition execution metadata is invalid.");
        }
    }

    private static bool HasAnimalIdentity(Guid? animalId, string? commonName, string? scientificName)
    {
        return animalId.HasValue || !string.IsNullOrWhiteSpace(commonName) || !string.IsNullOrWhiteSpace(scientificName);
    }
}
