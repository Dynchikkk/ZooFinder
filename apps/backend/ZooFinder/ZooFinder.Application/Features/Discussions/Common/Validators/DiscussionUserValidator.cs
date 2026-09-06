using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Discussions.Common.Validators;

public static class DiscussionUserValidator
{
    public static void ValidateUserAccountId(Guid userAccountId)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new RequestValidationException("User account ID must not be empty.");
        }
    }

    public static void ValidateUserCanWrite(UserAccount userAccount)
    {
        if (userAccount.IsDeleted)
        {
            throw new UnauthorizedException("User account was not found.");
        }

        if (userAccount.Status != UserStatus.Active)
        {
            throw new ForbiddenException("User account is not allowed to write messages.");
        }
    }
}
