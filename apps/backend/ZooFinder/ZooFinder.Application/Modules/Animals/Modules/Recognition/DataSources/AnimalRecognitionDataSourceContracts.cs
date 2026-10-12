using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Animals.Modules.Recognition.DataSources;

public sealed record ParkRecognitionContextResult(
    Guid Id,
    ParkStatus Status,
    IReadOnlyList<ParkRecognitionAnimalResult> Animals);

public sealed record ParkRecognitionAnimalResult(
    Guid ParkAnimalId,
    Guid AnimalId,
    string CommonName,
    string? ScientificName);
