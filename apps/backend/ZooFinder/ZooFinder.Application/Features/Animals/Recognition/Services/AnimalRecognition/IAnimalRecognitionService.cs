namespace ZooFinder.Application.Features.Animals.Recognition.Services.AnimalRecognition;

public interface IAnimalRecognitionService
{
    Task<AnimalRecognitionResponse> RecognizeAsync(
        AnimalRecognitionRequest request,
        CancellationToken cancellationToken);
}
