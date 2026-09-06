using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Application.Features.Discussions.Messages.Contracts;

public sealed record MessageHistoryRequest(
    Guid DiscussionRoomId,
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);
