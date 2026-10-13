using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Auth.DataSources;
using ZooFinder.Domain.Users;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Auth.DataSources;

public sealed class AuthDataSource : IAuthDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public AuthDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<UserAccount?> GetUserAccountByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return _dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Login == login, cancellationToken);
    }

    public async Task<bool> TryAddUserAccountAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userAccount);

        _dbContext.UserAccounts.Add(userAccount);

        try
        {
            // One SaveChanges transaction persists the complete registration graph.
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Resolve a competing registration by its natural key, without provider error codes.
            if (userAccount.Login != null &&
                await _dbContext.UserAccounts
                    .IgnoreQueryFilters()
                    .AnyAsync(account => account.Login == userAccount.Login && account.Id != userAccount.Id, cancellationToken))
            {
                return false;
            }

            throw;
        }
    }

    public async Task AddRefreshSessionAsync(UserRefreshSession refreshSession, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refreshSession);

        // The account is already persisted; attaching the navigation graph would insert it again.
        _dbContext.Entry(refreshSession).State = EntityState.Added;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<UserRefreshSession?> GetRefreshSessionWithUserAccountByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        return _dbContext.UserRefreshSessions
            .AsNoTracking()
            .Include(session => session.UserAccount)
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
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        UserRefreshSession? refreshSession = await _dbContext.UserRefreshSessions
            .SingleOrDefaultAsync(session => session.Id == refreshSessionId &&
                session.RefreshTokenHash == currentRefreshTokenHash && session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > usedAtUtc && session.UserAccount.Status == UserStatus.Active, cancellationToken);

        if (refreshSession == null)
        {
            return false;
        }

        refreshSession.RefreshTokenHash = newRefreshTokenHash;
        refreshSession.ExpiresAtUtc = expiresAtUtc;
        refreshSession.LastUsedAtUtc = usedAtUtc;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task RevokeRefreshSessionAsync(Guid refreshSessionId, DateTime revokedAtUtc, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        UserRefreshSession? refreshSession = await _dbContext.UserRefreshSessions
            .SingleOrDefaultAsync(session => session.Id == refreshSessionId && session.RevokedAtUtc == null, cancellationToken);

        if (refreshSession == null)
        {
            return;
        }

        refreshSession.RevokedAtUtc = revokedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RevokeAllRefreshSessionsAsync(
        Guid userAccountId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var refreshSessions = await _dbContext.UserRefreshSessions
            .IgnoreQueryFilters()
            .Where(session => session.UserAccountId == userAccountId && !session.IsDeleted && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (UserRefreshSession refreshSession in refreshSessions)
        {
            refreshSession.RevokedAtUtc = revokedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
