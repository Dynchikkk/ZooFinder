using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Modules.Parks.Modules.Animals.Services.ParkAnimals;
using ZooFinder.Application.Modules.Parks.Modules.Catalog.Services.Parks;
using ZooFinder.Infrastructure.Modules.Parks.DataSources;

namespace ZooFinder.Api.DependencyInjection;

internal static class ParksServiceCollectionExtensions
{
    public static IServiceCollection AddParksModule(this IServiceCollection services)
    {
        // Data sources
        services.AddScoped<IParkCatalogDataSource, ParkCatalogDataSource>();
        services.AddScoped<IParkAnimalDataSource, ParkAnimalDataSource>();

        // Services
        services.AddScoped<IParkService, ParkService>();
        services.AddScoped<IParkAnimalService, ParkAnimalService>();

        return services;
    }
}
