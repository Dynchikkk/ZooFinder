namespace ZooFinder.Application.Modules.Animals.Modules.Recognition.DataSources;

public interface IAnimalRecognitionDataSource
{
    // Only published, non-deleted animal links are included.
    Task<ParkRecognitionContextResult?> GetParkContextAsync(
        Guid parkId,
        CancellationToken cancellationToken);
}
