namespace ZooFinder.Application.Features.Animals.Recognition.Contracts;

public sealed record AnimalRecognitionResponse(
    bool IsAnimal,
    string? CommonName,
    string? ScientificName,
    IReadOnlyList<AnimalRecognitionCandidateResponse> Alternatives);

public sealed record AnimalRecognitionCandidateResponse(
    string? CommonName,
    string? ScientificName);
