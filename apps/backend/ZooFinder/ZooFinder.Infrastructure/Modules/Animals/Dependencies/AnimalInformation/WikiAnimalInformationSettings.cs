namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

public sealed class WikiAnimalInformationSettings
{
    public string UserAgent { get; init; } = string.Empty;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan SearchCacheLifetime { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan DetailsCacheLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public int MaximumResponseBytes { get; init; } = 2 * 1024 * 1024;
}
