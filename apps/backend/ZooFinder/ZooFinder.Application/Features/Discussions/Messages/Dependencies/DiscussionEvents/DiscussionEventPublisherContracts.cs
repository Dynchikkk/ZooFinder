namespace ZooFinder.Application.Features.Discussions.Messages.Dependencies.DiscussionEvents;

public sealed record DiscussionMessageEvent(
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
