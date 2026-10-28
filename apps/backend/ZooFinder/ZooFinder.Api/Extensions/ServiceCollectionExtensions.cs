using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ZooFinder.Api.Configuration;
using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Application.Modules.Auth.Dependencies.PasswordHashing;
using ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenGeneration;
using ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenHashing;
using ZooFinder.Application.Modules.Auth.Settings;
using ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.PasswordHashing;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.RefreshTokenGeneration;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.RefreshTokenHashing;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    private const string ConnectionStringName = "ZooFinder";
    private const string AuthSection = "Auth";
    private const string JwtSection = "Jwt";
    private const string WikiSection = "Wiki";
    private const string RecognitionSection = "Recognition";
    private const string MemoryCacheSection = "MemoryCache";

    public static IServiceCollection AddZooFinderDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddPersistence(configuration);
        services.AddAuthDependencies(configuration);
        services.AddAnimalProviders(configuration);
        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException("ConnectionStrings:ZooFinder is required.");
        ApiConfigurationValidator.ValidateConnectionString(connectionString);
        services.AddDbContext<ZooFinderDbContext>(options => options.UseSqlServer(connectionString));
    }

    private static void AddAuthDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        AuthSettings authSettings = GetSettings<AuthSettings>(configuration, AuthSection);
        ApiConfigurationValidator.ValidateAuthSettings(authSettings);
        services.AddSingleton(authSettings);
        services.AddSingleton(GetSettings<JwtAccessTokenSettings>(configuration, JwtSection));
        services.AddSingleton<IAccessTokenProvider, JwtAccessTokenProvider>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, CryptographicRefreshTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
    }

    private static void AddAnimalProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MemoryCacheOptions>()
            .Bind(configuration.GetRequiredSection(MemoryCacheSection))
            .Validate(options => options.SizeLimit.HasValue && options.SizeLimit.Value > 0,
                "MemoryCache:SizeLimit must be a positive number of cached responses.")
            .ValidateOnStart();
        services.AddMemoryCache();
        services.AddSingleton(GetSettings<WikiAnimalInformationSettings>(configuration, WikiSection));
        services.AddSingleton(GetSettings<PythonAnimalRecognitionSettings>(configuration, RecognitionSection));

        services.AddHttpClient<IAnimalInformationProvider, WikiAnimalInformationProvider>(ConfigureHttpClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);
        services.AddHttpClient<IAnimalRecognitionProvider, PythonAnimalRecognitionProvider>(ConfigureHttpClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);
    }

    private static TSettings GetSettings<TSettings>(IConfiguration configuration, string sectionName) where TSettings : class
    {
        return configuration.GetRequiredSection(sectionName).Get<TSettings>()
            ?? throw new InvalidOperationException($"Configuration section '{sectionName}' could not be read.");
    }

    private static void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = Timeout.InfiniteTimeSpan;
    }

    private static HttpMessageHandler CreateHttpHandler()
    {
        return new HttpClientHandler { AllowAutoRedirect = false };
    }
}
