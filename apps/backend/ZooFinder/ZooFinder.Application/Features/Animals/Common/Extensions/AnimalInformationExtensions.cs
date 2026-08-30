namespace ZooFinder.Application.Features.Animals.Common.Extensions;

public static class AnimalInformationExtensions
{
    public static string NormalizeInformationSource(this string? informationSource)
    {
        return informationSource?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public static string NormalizeSourceItemId(this string? sourceItemId)
    {
        return sourceItemId?.Trim() ?? string.Empty;
    }

    public static string NormalizeLanguageCode(this string? languageCode)
    {
        return languageCode?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
