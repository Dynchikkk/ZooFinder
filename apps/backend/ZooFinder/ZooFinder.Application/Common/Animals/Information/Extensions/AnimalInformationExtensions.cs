namespace ZooFinder.Application.Common.Animals.Information.Extensions;

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
}
