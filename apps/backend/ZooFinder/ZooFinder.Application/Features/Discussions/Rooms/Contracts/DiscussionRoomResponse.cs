using ZooFinder.Domain.Discussions;

namespace ZooFinder.Application.Features.Discussions.Rooms.Contracts;

public sealed record DiscussionRoomResponse(
    Guid Id,
    Guid AnimalId,
    DiscussionRoomType Type,
    string Name,
    string? Description,
    bool IsClosed);
