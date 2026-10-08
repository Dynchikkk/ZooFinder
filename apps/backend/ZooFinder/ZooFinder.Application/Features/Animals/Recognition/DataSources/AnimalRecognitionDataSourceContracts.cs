using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Animals.Recognition.DataSources;

public sealed record ParkRecognitionContextResult(
    Guid Id,
    ParkStatus Status,
    IReadOnlyList<ParkRecognitionAnimalResult> Animals);

public sealed record ParkRecognitionAnimalResult(
    Guid ParkAnimalId,
    Guid AnimalId,
    string CommonName,
    string? ScientificName);
