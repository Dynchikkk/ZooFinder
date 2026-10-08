namespace ZooFinder.Application.Features.Auth.Services.Auth;

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
