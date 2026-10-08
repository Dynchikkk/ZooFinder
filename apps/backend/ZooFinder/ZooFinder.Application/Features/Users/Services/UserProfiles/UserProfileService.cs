using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Users.DataSources;
using ZooFinder.Application.Features.Users.Validators;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Users.Services.UserProfiles;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserProfileDataSource _userProfileDataSource;

    public UserProfileService(IUserProfileDataSource userProfileDataSource)
    {
        ArgumentNullException.ThrowIfNull(userProfileDataSource);

        _userProfileDataSource = userProfileDataSource;
    }

    public async Task<UserProfileResponse> GetProfileAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        UserProfileValidator.ValidateUserAccountId(userAccountId);

        UserProfile userProfile = await GetUserProfileAsync(
            userAccountId,
            cancellationToken);

        return new UserProfileResponse(
            userProfile.UserAccountId,
            userProfile.DisplayName);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(
        Guid currentUserAccountId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        UserProfileValidator.ValidateUserAccountId(currentUserAccountId);

        string displayName = request.DisplayName?.Trim() ?? string.Empty;
        UserProfileValidator.ValidateDisplayName(displayName);

        UserProfile userProfile = await GetUserProfileAsync(
            currentUserAccountId,
            cancellationToken);

        userProfile.DisplayName = displayName;

        await _userProfileDataSource.UpdateAsync(userProfile, cancellationToken);

        return new UserProfileResponse(
            userProfile.UserAccountId,
            userProfile.DisplayName);
    }

    private async Task<UserProfile> GetUserProfileAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        UserProfile userProfile = await _userProfileDataSource.GetByUserAccountIdAsync(userAccountId, cancellationToken)
            ?? throw new NotFoundException("User profile was not found.");

        return userProfile;
    }
}
