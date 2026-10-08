namespace ZooFinder.Application.Features.Users.Services.UserProfiles;

public interface IUserProfileService
{
    Task<UserProfileResponse> GetProfileAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<UserProfileResponse> UpdateProfileAsync(
        Guid currentUserAccountId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken);
}
