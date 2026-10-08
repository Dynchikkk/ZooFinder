namespace ZooFinder.Application.Features.Animals.Recognition.DataSources;

public interface IAnimalRecognitionDataSource
{
    // Only published, non-deleted animal links are included.
    Task<ParkRecognitionContextResult?> GetParkContextAsync(
        Guid parkId,
        CancellationToken cancellationToken);
}
