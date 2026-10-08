namespace ZooFinder.Application.Features.Animals.Recognition.Dependencies.AnimalRecognition;

public enum AnimalRecognitionStatus
{
    Recognized = 1,
    Uncertain = 2,
    NoAnimal = 3
}

public sealed record AnimalRecognitionProviderRequest(
    Stream ImageStream, string ContentType, string LanguageCode,
    IReadOnlyList<AnimalRecognitionContextCandidate> Candidates)
{
    public bool HasParkContext => Candidates.Count > 0;
}

public sealed record AnimalRecognitionContextCandidate(Guid AnimalId, string CommonName, string? ScientificName);

public sealed record AnimalRecognitionProviderResult(
    AnimalRecognitionStatus Status, string? CommonName, string? ScientificName,
    IReadOnlyList<AnimalRecognitionProviderCandidate> Alternatives,
    Guid? AnimalId = null, AnimalRecognitionExecutionResult? Execution = null);

public sealed record AnimalRecognitionProviderCandidate(string? CommonName, string? ScientificName, Guid? AnimalId = null);

public sealed record AnimalRecognitionExecutionResult(string ModelId, string? Revision, double InferenceMilliseconds);
