namespace ZooFinder.Application.Features.Animals.Recognition.Contracts;

public sealed record AnimalRecognitionProviderResult(
    bool IsAnimal,
    string? CommonName,
    string? ScientificName,
    IReadOnlyList<AnimalRecognitionProviderCandidate> Alternatives);

public sealed record AnimalRecognitionProviderCandidate(
    string? CommonName,
    string? ScientificName);
