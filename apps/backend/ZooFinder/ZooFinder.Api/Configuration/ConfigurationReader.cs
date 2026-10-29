namespace ZooFinder.Api.Configuration;

internal static class ConfigurationReader
{
    public static TSettings GetSettings<TSettings>(IConfiguration configuration, string sectionName) where TSettings : class
    {
        return configuration.GetRequiredSection(sectionName).Get<TSettings>()
            ?? throw new InvalidOperationException($"Configuration section '{sectionName}' could not be read.");
    }
}
