using ZooFinder.Application.Shared.Language.Constants;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

namespace ZooFinder.Application.Modules.Animals.Modules.Recognition.Services.AnimalRecognition;

public sealed record AnimalRecognitionRequest(
    Stream ImageStream,
    string FileName,
    string ContentType,
    long Length,
    string LanguageCode = LanguageCodes.English,
    Guid? ParkId = null);

public sealed record AnimalRecognitionResponse(
    AnimalRecognitionStatus Status,
    string? CommonName,
    string? ScientificName,
    IReadOnlyList<AnimalRecognitionCandidateResponse> Alternatives,
    Guid? ParkAnimalId = null,
    AnimalRecognitionExecutionResult? Execution = null)
{
    public bool IsRecognized => Status == AnimalRecognitionStatus.Recognized;
}

public sealed record AnimalRecognitionCandidateResponse(
    string? CommonName,
    string? ScientificName,
    Guid? ParkAnimalId = null);
