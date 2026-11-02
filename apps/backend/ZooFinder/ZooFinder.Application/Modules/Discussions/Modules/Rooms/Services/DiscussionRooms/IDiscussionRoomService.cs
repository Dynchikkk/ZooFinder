namespace ZooFinder.Application.Modules.Discussions.Modules.Rooms.Services.DiscussionRooms;

public interface IDiscussionRoomService
{
    Task<DiscussionRoomResponse> GetRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken);

    Task<DiscussionRoomResponse?> GetGeneralRoomAsync(
        Guid animalId,
        CancellationToken cancellationToken);

    Task<DiscussionRoomResponse> CreateDiscussionAsync(
        Guid currentUserAccountId,
        CreateDiscussionRequest request,
        CancellationToken cancellationToken);
}
