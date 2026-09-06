using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Users.Constants;

namespace ZooFinder.Application.Features.Users.Validators;

public static class UserProfileValidator
{
    public static void ValidateDisplayName(string displayName)
    {
        if (displayName.Length < UserProfileConstraints.MinimumDisplayNameLength ||
            displayName.Length > UserProfileConstraints.MaximumDisplayNameLength)
        {
            throw new RequestValidationException(
                $"Display name length must be between {UserProfileConstraints.MinimumDisplayNameLength} " +
                $"and {UserProfileConstraints.MaximumDisplayNameLength} characters.");
        }
    }

    public static void ValidateUserAccountId(Guid userAccountId)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new RequestValidationException("User account ID must not be empty.");
        }
    }
}
