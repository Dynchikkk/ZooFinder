namespace ZooFinder.Application.Modules.Users.Modules.Profiles.Services.UserProfiles;

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
