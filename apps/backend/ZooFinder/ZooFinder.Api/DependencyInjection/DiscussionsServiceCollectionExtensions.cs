using ZooFinder.Api.Realtime.Discussions;
using ZooFinder.Application.Modules.Discussions.DataSources;
using ZooFinder.Application.Modules.Discussions.Modules.Messages.Dependencies.DiscussionEvents;
using ZooFinder.Application.Modules.Discussions.Modules.Messages.Services.DiscussionMessages;
using ZooFinder.Application.Modules.Discussions.Modules.Rooms.Services.DiscussionRooms;
using ZooFinder.Infrastructure.Modules.Discussions.DataSources;

namespace ZooFinder.Api.DependencyInjection;

internal static class DiscussionsServiceCollectionExtensions
{
    public static IServiceCollection AddDiscussionsModule(this IServiceCollection services)
    {
        // Transport
        services.AddSignalR();

        // Providers
        services.AddSingleton<IDiscussionEventPublisher, SignalRDiscussionEventPublisher>();

        // Data sources
        services.AddScoped<IDiscussionDataSource, DiscussionDataSource>();

        // Services
        services.AddScoped<IDiscussionRoomService, DiscussionRoomService>();
        services.AddScoped<IDiscussionMessageService, DiscussionMessageService>();

        return services;
    }
}
