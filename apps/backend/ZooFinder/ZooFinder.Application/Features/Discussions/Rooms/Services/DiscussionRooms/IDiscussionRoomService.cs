namespace ZooFinder.Application.Features.Discussions.Rooms.Services.DiscussionRooms;

public interface IDiscussionRoomService
{
    Task<DiscussionRoomResponse?> GetGeneralRoomAsync(
        Guid animalId,
        CancellationToken cancellationToken);

    Task<DiscussionRoomResponse> CreateDiscussionAsync(
        Guid currentUserAccountId,
        CreateDiscussionRequest request,
        CancellationToken cancellationToken);
}
