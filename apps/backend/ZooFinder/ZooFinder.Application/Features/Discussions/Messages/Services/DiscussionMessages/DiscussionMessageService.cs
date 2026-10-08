using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Common.Pagination.Extensions;
using ZooFinder.Application.Features.Discussions.Common.DataSources;
using ZooFinder.Application.Features.Discussions.Common.Validators;
using ZooFinder.Application.Features.Discussions.Messages.Dependencies.DiscussionEvents;
using ZooFinder.Application.Features.Discussions.Messages.Validators;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Discussions.Messages.Services.DiscussionMessages;

public sealed class DiscussionMessageService : IDiscussionMessageService
{
    private readonly IDiscussionDataSource _discussionDataSource;
    private readonly IDiscussionEventPublisher _discussionEventPublisher;
    private readonly TimeProvider _timeProvider;

    public DiscussionMessageService(
        IDiscussionDataSource discussionDataSource,
        IDiscussionEventPublisher discussionEventPublisher,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(discussionDataSource);
        ArgumentNullException.ThrowIfNull(discussionEventPublisher);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _discussionDataSource = discussionDataSource;
        _discussionEventPublisher = discussionEventPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<CursorPageResponse<DiscussionMessageResponse>> GetMessagesAsync(
        MessageHistoryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DiscussionMessageValidator.ValidateDiscussionRoomId(request.DiscussionRoomId);
        DiscussionMessageValidator.ValidatePageSize(request.Limit);

        DiscussionRoom room = await _discussionDataSource.GetRoomByIdAsync(request.DiscussionRoomId, cancellationToken)
            ?? throw new NotFoundException("Discussion room was not found.");

        if (room.IsDeleted)
        {
            throw new NotFoundException("Discussion room was not found.");
        }

        var pageRequest = new CursorPageRequest(request.Cursor.NormalizeCursor(), request.Limit);

        var page = await _discussionDataSource.GetMessagesWithAuthorProfilesAsync(room.Id, pageRequest, cancellationToken);

        var messages = new List<DiscussionMessageResponse>(page.Items.Count);

        foreach (DiscussionMessage message in page.Items)
        {
            UserProfile authorProfile = message.AuthorUserAccount?.UserProfile
                ?? throw new InvalidOperationException("Discussion message was loaded without its author profile.");

            messages.Add(new DiscussionMessageResponse(
                message.Id,
                message.DiscussionRoomId,
                message.AuthorUserAccountId,
                authorProfile.DisplayName,
                message.IsDeleted ? null : message.Content,
                message.EditedAtUtc != null,
                message.IsDeleted,
                message.CreatedAtUtc,
                message.EditedAtUtc,
                message.DeletedAtUtc));
        }

        return new CursorPageResponse<DiscussionMessageResponse>(messages, page.NextCursor);
    }

    public async Task<DiscussionMessageResponse> SendMessageAsync(
        Guid currentUserAccountId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DiscussionUserValidator.ValidateUserAccountId(currentUserAccountId);
        DiscussionMessageValidator.ValidateDiscussionRoomId(request.DiscussionRoomId);

        string content = request.Content?.Trim() ?? string.Empty;
        DiscussionMessageValidator.ValidateContent(content);

        UserAccount author = await GetActiveUserAccountAsync(currentUserAccountId, cancellationToken);

        DiscussionRoom room = await _discussionDataSource.GetRoomByIdAsync(request.DiscussionRoomId, cancellationToken)
            ?? throw new NotFoundException("Discussion room was not found.");

        DiscussionMessageValidator.ValidateRoomWritable(room);

        return await AddMessageAsync(room, author, content, cancellationToken);
    }

    public async Task<DiscussionMessageResponse> EditMessageAsync(
        Guid currentUserAccountId,
        EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DiscussionUserValidator.ValidateUserAccountId(currentUserAccountId);
        DiscussionMessageValidator.ValidateDiscussionMessageId(request.DiscussionMessageId);

        string content = request.Content?.Trim() ?? string.Empty;
        DiscussionMessageValidator.ValidateContent(content);

        UserAccount currentUser = await GetActiveUserAccountAsync(currentUserAccountId, cancellationToken);

        DiscussionMessage message = await _discussionDataSource
            .GetMessageWithRoomAndAuthorProfileByIdAsync(request.DiscussionMessageId, cancellationToken)
            ?? throw new NotFoundException("Discussion message was not found.");

        if (message.IsDeleted)
        {
            throw new NotFoundException("Discussion message was not found.");
        }

        DiscussionRoom room = message.DiscussionRoom
            ?? throw new InvalidOperationException("Discussion message was loaded without its discussion room.");

        UserProfile authorProfile = message.AuthorUserAccount?.UserProfile
            ?? throw new InvalidOperationException("Discussion message was loaded without its author profile.");

        DiscussionMessageValidator.ValidateRoomWritable(room);
        DiscussionMessageValidator.ValidateMessagePermission(message, currentUser);

        DateTime currentTime = GetCurrentTime();
        message.Content = content;
        message.EditedAtUtc = currentTime;
        message.UpdatedAtUtc = currentTime;

        await _discussionDataSource.UpdateMessageAsync(message, cancellationToken);

        var response = new DiscussionMessageResponse(
            message.Id,
            message.DiscussionRoomId,
            message.AuthorUserAccountId,
            authorProfile.DisplayName,
            message.Content,
            true,
            false,
            message.CreatedAtUtc,
            message.EditedAtUtc,
            null);

        await _discussionEventPublisher.PublishMessageUpdatedAsync(ToEvent(response), cancellationToken);

        return response;
    }

    public async Task DeleteMessageAsync(
        Guid currentUserAccountId,
        DeleteMessageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DiscussionUserValidator.ValidateUserAccountId(currentUserAccountId);
        DiscussionMessageValidator.ValidateDiscussionMessageId(request.DiscussionMessageId);

        UserAccount currentUser = await GetActiveUserAccountAsync(currentUserAccountId, cancellationToken);

        DiscussionMessage message = await _discussionDataSource
            .GetMessageWithRoomAndAuthorProfileByIdAsync(request.DiscussionMessageId, cancellationToken)
            ?? throw new NotFoundException("Discussion message was not found.");

        if (message.IsDeleted)
        {
            throw new NotFoundException("Discussion message was not found.");
        }

        DiscussionRoom room = message.DiscussionRoom
            ?? throw new InvalidOperationException("Discussion message was loaded without its discussion room.");

        DiscussionMessageValidator.ValidateRoomWritable(room);
        DiscussionMessageValidator.ValidateMessagePermission(message, currentUser);

        DateTime currentTime = GetCurrentTime();
        message.IsDeleted = true;
        message.DeletedAtUtc = currentTime;
        message.UpdatedAtUtc = currentTime;

        await _discussionDataSource.UpdateMessageAsync(message, cancellationToken);

        await _discussionEventPublisher.PublishMessageDeletedAsync(
            message.DiscussionRoomId,
            message.Id,
            currentTime,
            cancellationToken);
    }

    private async Task<DiscussionMessageResponse> AddMessageAsync(
        DiscussionRoom room,
        UserAccount author,
        string content,
        CancellationToken cancellationToken)
    {
        DateTime currentTime = GetCurrentTime();

        var message = new DiscussionMessage
        {
            Id = Guid.NewGuid(),
            DiscussionRoomId = room.Id,
            DiscussionRoom = room,
            AuthorUserAccountId = author.Id,
            AuthorUserAccount = author,
            Content = content,
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        await _discussionDataSource.AddMessageAsync(message, cancellationToken);

        var response = new DiscussionMessageResponse(
            message.Id,
            room.Id,
            author.Id,
            author.UserProfile.DisplayName,
            message.Content,
            false,
            false,
            message.CreatedAtUtc,
            null,
            null);

        await _discussionEventPublisher.PublishMessageCreatedAsync(ToEvent(response), cancellationToken);

        return response;
    }

    private async Task<UserAccount> GetActiveUserAccountAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        UserAccount userAccount = await _discussionDataSource.GetUserAccountWithProfileByIdAsync(userAccountId, cancellationToken)
            ?? throw new UnauthorizedException("User account was not found.");

        DiscussionUserValidator.ValidateUserCanWrite(userAccount);

        _ = userAccount.UserProfile ?? throw new InvalidOperationException("User account was loaded without its profile.");

        return userAccount;
    }

    private static DiscussionMessageEvent ToEvent(DiscussionMessageResponse message)
    {
        return new DiscussionMessageEvent(
            message.Id,
            message.DiscussionRoomId,
            message.AuthorUserAccountId,
            message.AuthorDisplayName,
            message.Content,
            message.IsEdited,
            message.IsDeleted,
            message.CreatedAtUtc,
            message.EditedAtUtc,
            message.DeletedAtUtc);
    }

    private DateTime GetCurrentTime()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }
}
