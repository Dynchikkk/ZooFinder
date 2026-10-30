using ZooFinder.Api.Configuration;
using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Modules.Animals.Modules.Catalog.DataSources;
using ZooFinder.Application.Modules.Animals.Modules.Catalog.Services.AnimalCatalog;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Catalog.DataSources;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

namespace ZooFinder.Api.DependencyInjection;

internal static class AnimalsServiceCollectionExtensions
{
    private const string WikiSection = "Wiki";
    private const string RecognitionSection = "Recognition";

    public static IServiceCollection AddAnimalsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.AddSingleton(
            ConfigurationReader.GetSettings<PythonAnimalRecognitionSettings>(configuration, RecognitionSection));
        services.AddSingleton(ConfigurationReader.GetSettings<WikiAnimalInformationSettings>(configuration, WikiSection));
        
        // Providers
        services.AddHttpClient<IAnimalInformationProvider, WikiAnimalInformationProvider>(ConfigureHttpClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);
        services.AddHttpClient<IAnimalRecognitionProvider, PythonAnimalRecognitionProvider>(ConfigureHttpClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);
        
        // Data sources
        services.AddScoped<IAnimalCatalogDataSource, AnimalCatalogDataSource>();

        // Services
        services.AddScoped<IAnimalCatalogService, AnimalCatalogService>();

        return services;
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
