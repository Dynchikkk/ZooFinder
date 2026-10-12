using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Modules.Users.Modules.Profiles.DataSources;
using ZooFinder.Domain.Users;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Users.Modules.Profiles.DataSources;

public sealed class UserProfileDataSource : IUserProfileDataSource
{
    private readonly ZooFinderDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UserProfileDataSource(ZooFinderDbContext dbContext, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<UserProfile?> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        return _dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserAccountId == userAccountId, cancellationToken);
    }

    public async Task UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        int count = await _dbContext.UserProfiles
            .Where(profile => profile.Id == userProfile.Id && profile.UserAccountId == userProfile.UserAccountId &&
                profile.UserAccount.Status == UserStatus.Active)
            .ExecuteUpdateAsync(setters => setters.SetProperty(profile => profile.DisplayName, userProfile.DisplayName)
                .SetProperty(profile => profile.UpdatedAtUtc, now), cancellationToken);
        if (count == 0)
        {
            throw new ForbiddenException("User profile is no longer available for update.");
        }

        userProfile.UpdatedAtUtc = now;
    }
}
