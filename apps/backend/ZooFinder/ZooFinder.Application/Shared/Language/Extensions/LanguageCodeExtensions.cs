namespace ZooFinder.Application.Shared.Language.Extensions;

public static class LanguageCodeExtensions
{
    public static string NormalizeLanguageCode(this string? languageCode)
    {
        return languageCode?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
