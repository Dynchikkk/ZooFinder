using ZooFinder.Api.Configuration;
using ZooFinder.Application.Common.Animals.DataSources;
using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Common.Animals.Services.AnimalRegistration;
using ZooFinder.Application.Modules.Animals.Modules.Catalog.DataSources;
using ZooFinder.Application.Modules.Animals.Modules.Catalog.Services.AnimalCatalog;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.DataSources;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Services.AnimalRecognition;
using ZooFinder.Infrastructure.Modules.Animals.DataSources;
using ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;
using ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation.Settings;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Catalog.DataSources;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.DataSources;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition.Settings;

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
        services.AddScoped<IAnimalRegistrationDataSource, AnimalRegistrationDataSource>();
        services.AddScoped<IAnimalCatalogDataSource, AnimalCatalogDataSource>();
        services.AddScoped<IAnimalRecognitionDataSource, AnimalRecognitionDataSource>();

        // Services
        services.AddScoped<IAnimalRegistrationService, AnimalRegistrationService>();
        services.AddScoped<IAnimalCatalogService, AnimalCatalogService>();
        services.AddScoped<IAnimalRecognitionService, AnimalRecognitionService>();

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
