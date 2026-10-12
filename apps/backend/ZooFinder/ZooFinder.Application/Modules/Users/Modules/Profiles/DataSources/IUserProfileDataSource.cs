using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Modules.Users.Modules.Profiles.DataSources;

public interface IUserProfileDataSource
{
    Task<UserProfile?> GetByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        UserProfile userProfile,
        CancellationToken cancellationToken);
}
