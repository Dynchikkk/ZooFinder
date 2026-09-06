namespace ZooFinder.Application.Features.Discussions.Messages.Contracts;

public sealed record DiscussionMessageResponse(
    Guid Id,
    Guid DiscussionRoomId,
    Guid AuthorUserAccountId,
    string AuthorDisplayName,
    string? Content,
    bool IsEdited,
    bool IsDeleted,
    DateTime CreatedAtUtc,
    DateTime? EditedAtUtc,
    DateTime? DeletedAtUtc);
