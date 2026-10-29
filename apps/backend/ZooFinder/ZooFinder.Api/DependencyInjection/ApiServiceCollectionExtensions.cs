namespace ZooFinder.Api.DependencyInjection;

internal static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddZooFinderApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Base
        services.AddControllers();
        services.AddOpenApi();

        // Time
        services.AddSingleton(TimeProvider.System);

        // Infrastructure
        services.AddZooFinderErrorHandling();
        services.AddPersistence(configuration);
        services.AddCaching(configuration);
        
        // Modules
        services.AddAuthModule(configuration);
        services.AddAnimalsModule(configuration);

        return services;
    }
}
