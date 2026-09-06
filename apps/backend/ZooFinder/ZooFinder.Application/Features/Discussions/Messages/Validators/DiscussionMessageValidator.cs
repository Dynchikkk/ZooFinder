using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Discussions.Messages.Constants;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Discussions.Messages.Validators;

public static class DiscussionMessageValidator
{
    public static void ValidateContent(string content)
    {
        if (content.Length < DiscussionMessageConstraints.MinimumContentLength ||
            content.Length > DiscussionMessageConstraints.MaximumContentLength)
        {
            throw new RequestValidationException(
                $"Message length must be between {DiscussionMessageConstraints.MinimumContentLength} " +
                $"and {DiscussionMessageConstraints.MaximumContentLength} characters.");
        }
    }

    public static void ValidateDiscussionRoomId(Guid discussionRoomId)
    {
        if (discussionRoomId == Guid.Empty)
        {
            throw new RequestValidationException("Discussion room ID must not be empty.");
        }
    }

    public static void ValidateDiscussionMessageId(Guid discussionMessageId)
    {
        if (discussionMessageId == Guid.Empty)
        {
            throw new RequestValidationException("Discussion message ID must not be empty.");
        }
    }

    public static void ValidatePageSize(int limit)
    {
        if (limit < 1 || limit > PaginationDefaults.MaximumPageSize)
        {
            throw new RequestValidationException(
                $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.");
        }
    }

    public static void ValidateRoomWritable(DiscussionRoom room)
    {
        if (room.IsDeleted)
        {
            throw new NotFoundException("Discussion room was not found.");
        }

        if (room.IsClosed)
        {
            throw new ConflictException("Discussion room is closed.");
        }
    }

    public static void ValidateMessagePermission(
        DiscussionMessage message,
        UserAccount currentUser)
    {
        bool isAuthor = message.AuthorUserAccountId == currentUser.Id;
        bool isModerator = currentUser.Role == UserRole.Moderator ||
                           currentUser.Role == UserRole.Administrator;

        if (!isAuthor && !isModerator)
        {
            throw new ForbiddenException(
                "User account is not allowed to modify this message.");
        }
    }
}
