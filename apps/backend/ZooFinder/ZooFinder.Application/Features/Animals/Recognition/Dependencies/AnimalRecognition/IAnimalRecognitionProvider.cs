namespace ZooFinder.Application.Features.Animals.Recognition.Dependencies.AnimalRecognition;

public interface IAnimalRecognitionProvider
{
    Task<AnimalRecognitionProviderResult> RecognizeAsync(
        AnimalRecognitionProviderRequest request,
        CancellationToken cancellationToken);
}
