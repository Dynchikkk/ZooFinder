using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Users.Modules.Profiles.DataSources;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Domain.Users;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Users.Modules.Profiles.DataSources;

public sealed class UserProfileDataSource : IUserProfileDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public UserProfileDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<UserProfile?> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        return _dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserAccountId == userAccountId, cancellationToken);
    }

    public async Task UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userProfile);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        UserProfile? existingProfile = await _dbContext.UserProfiles
            .SingleOrDefaultAsync(profile => profile.Id == userProfile.Id &&
                profile.UserAccountId == userProfile.UserAccountId && profile.UserAccount.Status == UserStatus.Active,
                cancellationToken);

        if (existingProfile == null)
        {
            throw new ForbiddenException("User profile is no longer available for update.");
        }

        existingProfile.DisplayName = userProfile.DisplayName;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            userProfile.UpdatedAtUtc = existingProfile.UpdatedAtUtc;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ForbiddenException("User profile is no longer available for update.");
        }
    }
}
