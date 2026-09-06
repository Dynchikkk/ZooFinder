using ZooFinder.Application.Features.Auth.Contracts;

namespace ZooFinder.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<AuthResponse> RefreshSessionAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken);

    Task RevokeSessionAsync(
        RevokeSessionRequest request,
        CancellationToken cancellationToken);

    Task RevokeAllSessionsAsync(
        Guid currentUserAccountId,
        CancellationToken cancellationToken);
}
