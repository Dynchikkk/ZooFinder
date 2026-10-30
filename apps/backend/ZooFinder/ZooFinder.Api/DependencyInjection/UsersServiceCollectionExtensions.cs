using ZooFinder.Application.Modules.Users.Modules.Profiles.DataSources;
using ZooFinder.Application.Modules.Users.Modules.Profiles.Services.UserProfiles;
using ZooFinder.Infrastructure.Modules.Users.Modules.Profiles.DataSources;

namespace ZooFinder.Api.DependencyInjection;

internal static class UsersServiceCollectionExtensions
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // Data sources
        services.AddScoped<IUserProfileDataSource, UserProfileDataSource>();

        // Services
        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }
}
