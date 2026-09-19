using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Features.Auth.Interfaces;
using ZooFinder.Domain.Users;

namespace ZooFinder.Infrastructure.Persistence.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly ZooFinderDbContext _dbContext;

    public AuthRepository(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<UserAccount?> GetUserAccountByLoginAsync(
        string login,
        CancellationToken cancellationToken)
    {
        return _dbContext.UserAccounts.AsNoTracking()
            .SingleOrDefaultAsync(account => account.Login == login, cancellationToken);
    }

    public async Task<bool> TryAddUserAccountAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken)
    {
        _dbContext.UserAccounts.Add(userAccount);
        var addedEntries = _dbContext.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added &&
                (ReferenceEquals(entry.Entity, userAccount) ||
                 ReferenceEquals(entry.Entity, userAccount.UserProfile) ||
                 userAccount.UserRefreshSessions.Any(session => ReferenceEquals(entry.Entity, session))))
            .ToArray();

        try
        {
            // One SaveChanges transaction persists the complete registration graph.
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            foreach (var entry in addedEntries)
            {
                entry.State = EntityState.Detached;
            }

            // Resolve a competing registration by its natural key, without provider error codes.
            if (userAccount.Login != null &&
                await _dbContext.UserAccounts.IgnoreQueryFilters().AnyAsync(
                    account => account.Login == userAccount.Login && account.Id != userAccount.Id,
                    cancellationToken))
            {
                return false;
            }

            throw;
        }
    }

    public async Task AddRefreshSessionAsync(
        UserRefreshSession refreshSession,
        CancellationToken cancellationToken)
    {
        // The account is already persisted; attaching the navigation graph would insert it again.
        _dbContext.Entry(refreshSession).State = EntityState.Added;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<UserRefreshSession?> GetRefreshSessionWithUserAccountByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        return _dbContext.UserRefreshSessions.AsNoTracking().Include(session => session.UserAccount)
            .SingleOrDefaultAsync(session => session.RefreshTokenHash == refreshTokenHash, cancellationToken);
    }

    public async Task<bool> TryRotateRefreshSessionAsync(
        Guid refreshSessionId,
        string currentRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime expiresAtUtc,
        DateTime usedAtUtc,
        CancellationToken cancellationToken)
    {
        int count = await _dbContext.UserRefreshSessions
            .Where(session => session.Id == refreshSessionId &&
                session.RefreshTokenHash == currentRefreshTokenHash &&
                session.RevokedAtUtc == null && session.ExpiresAtUtc > usedAtUtc &&
                session.UserAccount.Status == UserStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RefreshTokenHash, newRefreshTokenHash)
                .SetProperty(session => session.ExpiresAtUtc, expiresAtUtc)
                .SetProperty(session => session.LastUsedAtUtc, usedAtUtc)
                .SetProperty(session => session.UpdatedAtUtc, usedAtUtc), cancellationToken);
        return count == 1;
    }

    public async Task RevokeRefreshSessionAsync(
        Guid refreshSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await _dbContext.UserRefreshSessions
            .Where(session => session.Id == refreshSessionId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, revokedAtUtc)
                .SetProperty(session => session.UpdatedAtUtc, revokedAtUtc), cancellationToken);
    }

    public async Task RevokeAllRefreshSessionsAsync(
        Guid userAccountId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await _dbContext.UserRefreshSessions.IgnoreQueryFilters()
            .Where(session => session.UserAccountId == userAccountId &&
                !session.IsDeleted && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, revokedAtUtc)
                .SetProperty(session => session.UpdatedAtUtc, revokedAtUtc), cancellationToken);
    }
}
