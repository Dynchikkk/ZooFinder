namespace ZooFinder.Application.Modules.Parks.Extensions;

public static class ParkTextExtensions
{
    public static string NormalizeText(this string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static string? NormalizeOptionalText(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
