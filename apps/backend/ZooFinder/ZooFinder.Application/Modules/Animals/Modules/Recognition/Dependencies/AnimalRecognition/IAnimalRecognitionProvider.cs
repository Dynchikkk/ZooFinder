namespace ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

public interface IAnimalRecognitionProvider
{
    Task<AnimalRecognitionProviderResult> RecognizeAsync(
        AnimalRecognitionProviderRequest request,
        CancellationToken cancellationToken);
}
