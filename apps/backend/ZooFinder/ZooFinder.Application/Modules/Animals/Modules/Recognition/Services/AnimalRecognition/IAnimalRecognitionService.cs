namespace ZooFinder.Application.Modules.Animals.Modules.Recognition.Services.AnimalRecognition;

public interface IAnimalRecognitionService
{
    Task<AnimalRecognitionResponse> RecognizeAsync(
        AnimalRecognitionRequest request,
        CancellationToken cancellationToken);
}
