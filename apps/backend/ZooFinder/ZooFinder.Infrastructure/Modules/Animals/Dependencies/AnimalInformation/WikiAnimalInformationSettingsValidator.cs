namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

internal static class WikiAnimalInformationSettingsValidator
{
    private const int MinimumResponseLimitBytes = 1024;
    private const int MaximumResponseLimitBytes = 16 * 1024 * 1024;

    private static readonly TimeSpan MinimumRequestTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumRequestTimeout = TimeSpan.FromMinutes(2);

    public static void Validate(WikiAnimalInformationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.UserAgent))
        {
            throw new ArgumentException(
                "A descriptive Wikipedia User-Agent with contact information is required.", nameof(settings));
        }

        // Use the HTTP header parser to validate the complete User-Agent value without sending a request.
        using var request = new HttpRequestMessage();
        request.Headers.UserAgent.ParseAdd(settings.UserAgent);

        if (settings.RequestTimeout < MinimumRequestTimeout || settings.RequestTimeout > MaximumRequestTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Wikipedia request timeout is out of range.");
        }

        if (settings.SearchCacheLifetime <= TimeSpan.Zero || settings.DetailsCacheLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Wikipedia cache lifetimes must be positive.");
        }

        if (settings.MaximumResponseBytes < MinimumResponseLimitBytes ||
            settings.MaximumResponseBytes > MaximumResponseLimitBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Wikipedia response size limit is out of range.");
        }
    }
}
