using ZooFinder.Application.Features.Animals.Recognition.Contracts;

namespace ZooFinder.Application.Features.Animals.Recognition.Interfaces;

public interface IAnimalRecognitionProvider
{
    Task<AnimalRecognitionProviderResult> RecognizeAsync(
        Stream imageStream,
        string contentType,
        string languageCode,
        CancellationToken cancellationToken);
}
