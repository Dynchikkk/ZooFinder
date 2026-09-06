using ZooFinder.Application.Features.Discussions.Rooms.Contracts;

namespace ZooFinder.Application.Features.Discussions.Rooms.Interfaces;

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
