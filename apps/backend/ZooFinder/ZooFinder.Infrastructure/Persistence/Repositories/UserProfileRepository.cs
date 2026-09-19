using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Users.Interfaces;
using ZooFinder.Domain.Users;

namespace ZooFinder.Infrastructure.Persistence.Repositories;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly ZooFinderDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UserProfileRepository(
        ZooFinderDbContext dbContext,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<UserProfile?> GetByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return _dbContext.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserAccountId == userAccountId, cancellationToken);
    }

    public async Task UpdateAsync(
        UserProfile userProfile,
        CancellationToken cancellationToken)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        int count = await _dbContext.UserProfiles
            .Where(profile => profile.Id == userProfile.Id &&
                profile.UserAccountId == userProfile.UserAccountId &&
                profile.UserAccount.Status == UserStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(profile => profile.DisplayName, userProfile.DisplayName)
                .SetProperty(profile => profile.UpdatedAtUtc, now), cancellationToken);
        if (count == 0)
        {
            throw new ForbiddenException("User profile is no longer available for update.");
        }

        userProfile.UpdatedAtUtc = now;
    }
}
