using ZooFinder.Application.Shared.Language.Constants;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Application.Modules.Discussions.Modules.Rooms.Services.DiscussionRooms;

public sealed record CreateDiscussionRequest(
    string InformationSource,
    string SourceItemId,
    string LanguageCode = LanguageCodes.English);

public sealed record DiscussionRoomResponse(
    Guid Id,
    Guid AnimalId,
    DiscussionRoomType Type,
    string Name,
    string? Description,
    bool IsClosed);
