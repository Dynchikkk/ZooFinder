namespace ZooFinder.Application.Common.Language.Constants;

public static class LanguageCodes
{
    public const string English = "en";
    public const string Russian = "ru";

    public static bool IsSupported(string? languageCode)
    {
        return string.Equals(languageCode, English, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(languageCode, Russian, StringComparison.OrdinalIgnoreCase);
    }
}
