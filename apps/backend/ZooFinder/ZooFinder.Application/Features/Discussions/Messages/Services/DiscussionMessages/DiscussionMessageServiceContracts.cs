using ZooFinder.Application.Common.Pagination.Constants;

namespace ZooFinder.Application.Features.Discussions.Messages.Services.DiscussionMessages;

public sealed record DeleteMessageRequest(Guid DiscussionMessageId);

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

public sealed record EditMessageRequest(
    Guid DiscussionMessageId,
    string Content);

public sealed record MessageHistoryRequest(
    Guid DiscussionRoomId,
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);

public sealed record SendMessageRequest(
    Guid DiscussionRoomId,
    string Content);
