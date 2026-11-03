namespace ZooFinder.Api.DependencyInjection;

internal static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddZooFinderLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddJsonConsole();
        });

        return services;
    }
}
