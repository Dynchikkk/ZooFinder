using ZooFinder.Application.Features.Animals.Recognition.Contracts;

namespace ZooFinder.Application.Features.Animals.Recognition.Interfaces;

public interface IAnimalRecognitionService
{
    Task<AnimalRecognitionResponse> RecognizeAsync(
        AnimalRecognitionRequest request,
        CancellationToken cancellationToken);
}
