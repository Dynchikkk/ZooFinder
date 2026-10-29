using Microsoft.Extensions.Caching.Memory;

namespace ZooFinder.Api.DependencyInjection;

internal static class CachingServiceCollectionExtensions
{
    private const string MemoryCacheSection = "MemoryCache";

    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.AddOptions<MemoryCacheOptions>()
            .Bind(configuration.GetRequiredSection(MemoryCacheSection))
            .Validate(options => options.SizeLimit.HasValue && options.SizeLimit.Value > 0,
                "MemoryCache:SizeLimit must be a positive number of cached responses.")
            .ValidateOnStart();

        // Services
        services.AddMemoryCache();

        return services;
    }
}
