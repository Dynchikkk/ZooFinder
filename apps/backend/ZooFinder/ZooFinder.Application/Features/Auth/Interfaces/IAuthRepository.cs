using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Auth.Interfaces;

public interface IAuthRepository
{
    Task<UserAccount?> GetUserAccountByLoginAsync(
        string login,
        CancellationToken cancellationToken);

    Task<bool> TryAddUserAccountAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken);

    Task AddRefreshSessionAsync(
        UserRefreshSession refreshSession,
        CancellationToken cancellationToken);

    Task<UserRefreshSession?> GetRefreshSessionWithUserAccountByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken);

    Task<bool> TryRotateRefreshSessionAsync(
        Guid refreshSessionId,
        string currentRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime expiresAtUtc,
        DateTime usedAtUtc,
        CancellationToken cancellationToken);

    Task RevokeRefreshSessionAsync(
        Guid refreshSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken);

    Task RevokeAllRefreshSessionsAsync(
        Guid userAccountId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken);
}
