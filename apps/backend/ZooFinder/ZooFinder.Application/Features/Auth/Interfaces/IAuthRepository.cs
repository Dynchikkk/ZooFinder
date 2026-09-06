using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Auth.Interfaces;

public interface IAuthRepository
{
    Task<bool> IsLoginTakenAsync(
        string login,
        CancellationToken cancellationToken);

    Task<UserAccount?> GetUserAccountByLoginAsync(
        string login,
        CancellationToken cancellationToken);

    Task AddUserAccountAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken);

    Task AddRefreshSessionAsync(
        UserRefreshSession refreshSession,
        CancellationToken cancellationToken);

    Task<UserRefreshSession?> GetRefreshSessionWithUserAccountByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken);

    Task UpdateRefreshSessionAsync(
        UserRefreshSession refreshSession,
        CancellationToken cancellationToken);

    Task RevokeAllRefreshSessionsAsync(
        Guid userAccountId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken);
}
