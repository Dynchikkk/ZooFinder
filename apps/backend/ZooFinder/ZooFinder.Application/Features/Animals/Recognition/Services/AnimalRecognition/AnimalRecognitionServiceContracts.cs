using ZooFinder.Application.Common.Language.Constants;
using ZooFinder.Application.Features.Animals.Recognition.Dependencies.AnimalRecognition;

namespace ZooFinder.Application.Features.Animals.Recognition.Services.AnimalRecognition;

public sealed record AnimalRecognitionRequest(
    Stream ImageStream,
    string FileName,
    string ContentType,
    long Length,
    string LanguageCode = LanguageCodes.English,
    Guid? ParkId = null);

public sealed record AnimalRecognitionResponse(
    AnimalRecognitionStatus Status, string? CommonName, string? ScientificName,
    IReadOnlyList<AnimalRecognitionCandidateResponse> Alternatives, Guid? ParkAnimalId = null,
    AnimalRecognitionExecutionResult? Execution = null)
{
    public bool IsRecognized => Status == AnimalRecognitionStatus.Recognized;
}

public sealed record AnimalRecognitionCandidateResponse(string? CommonName, string? ScientificName, Guid? ParkAnimalId = null);
