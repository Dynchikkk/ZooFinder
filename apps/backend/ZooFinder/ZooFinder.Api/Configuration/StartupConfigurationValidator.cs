using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Application.Modules.Auth.Modules.Authentication.Services.Auth;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Api.Configuration;

internal static class StartupConfigurationValidator
{
    public static void Validate(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        services.GetRequiredService<ZooFinderDbContext>();
        services.GetRequiredService<IAccessTokenProvider>();
        services.GetRequiredService<IAuthService>();
        services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        services.GetRequiredService<IAnimalInformationProvider>();
        services.GetRequiredService<IAnimalRecognitionProvider>();
    }
}
