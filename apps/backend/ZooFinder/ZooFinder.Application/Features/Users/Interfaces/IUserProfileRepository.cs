using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Users.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        UserProfile userProfile,
        CancellationToken cancellationToken);
}
