using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Users.Contracts;
using ZooFinder.Application.Features.Users.Interfaces;
using ZooFinder.Application.Features.Users.Validators;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Users.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _userProfileRepository;

    public UserProfileService(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
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

        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);

        return new UserProfileResponse(
            userProfile.UserAccountId,
            userProfile.DisplayName);
    }

    private async Task<UserProfile> GetUserProfileAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        UserProfile? userProfile = await _userProfileRepository.GetByUserAccountIdAsync(userAccountId, cancellationToken) 
            ?? throw new NotFoundException("User profile was not found.");
        return userProfile;
    }
}
