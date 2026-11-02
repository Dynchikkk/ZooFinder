using ZooFinder.Api.Configuration.Parks.Settings;
using ZooFinder.Api.Controllers.Parks.Import;
using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Modules.Parks.Modules.Animals.Services.ParkAnimals;
using ZooFinder.Application.Modules.Parks.Modules.Catalog.Services.Parks;
using ZooFinder.Application.Modules.Parks.Modules.Import.Services.ParkAnimalImport;
using ZooFinder.Infrastructure.Modules.Parks.DataSources;

namespace ZooFinder.Api.DependencyInjection;

internal static class ParksServiceCollectionExtensions
{
    private const string ImportSection = "ParkImport";

    public static IServiceCollection AddParksModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.AddOptions<ParkImportSettings>()
            .Bind(configuration.GetRequiredSection(ImportSection))
            .Validate(settings => settings.MaximumFileBytes > 0, "ParkImport:MaximumFileBytes must be positive.")
            .ValidateOnStart();

        // Transport
        services.AddScoped<ParkAnimalCsvReader>();

        // Data sources
        services.AddScoped<IParkCatalogDataSource, ParkCatalogDataSource>();
        services.AddScoped<IParkAnimalDataSource, ParkAnimalDataSource>();

        // Services
        services.AddScoped<IParkService, ParkService>();
        services.AddScoped<IParkAnimalService, ParkAnimalService>();
        services.AddScoped<IParkAnimalImportService, ParkAnimalImportService>();

        return services;
    }
}
