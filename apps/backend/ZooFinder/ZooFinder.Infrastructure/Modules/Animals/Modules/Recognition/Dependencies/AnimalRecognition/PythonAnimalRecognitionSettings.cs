namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

public sealed class PythonAnimalRecognitionSettings
{
    public Uri? BaseUrl { get; init; }
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(60);
    public int MaximumResponseBytes { get; init; } = 1024 * 1024;
    public IReadOnlyList<string> SupportedImageTypes { get; init; } = [];
}
