using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Api.Extensions;

internal static class ApplicationBuilderExtensions
{
    public static void ValidateZooFinderConfiguration(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        services.GetRequiredService<ZooFinderDbContext>();
        services.GetRequiredService<IAccessTokenProvider>();
        services.GetRequiredService<IAnimalInformationProvider>();
        services.GetRequiredService<IAnimalRecognitionProvider>();
    }
}
