using ZooFinder.Application.Features.Users.Contracts;

namespace ZooFinder.Application.Features.Users.Interfaces;

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
