using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Discussions.Messages.Contracts;

namespace ZooFinder.Application.Features.Discussions.Messages.Interfaces;

public interface IDiscussionMessageService
{
    Task<CursorPageResponse<DiscussionMessageResponse>> GetMessagesAsync(
        MessageHistoryRequest request,
        CancellationToken cancellationToken);

    Task<DiscussionMessageResponse> SendMessageAsync(
        Guid currentUserAccountId,
        SendMessageRequest request,
        CancellationToken cancellationToken);

    Task<DiscussionMessageResponse> EditMessageAsync(
        Guid currentUserAccountId,
        EditMessageRequest request,
        CancellationToken cancellationToken);

    Task DeleteMessageAsync(
        Guid currentUserAccountId,
        DeleteMessageRequest request,
        CancellationToken cancellationToken);
}
