namespace ZooFinder.Application.Features.Auth.Services.Auth;

public sealed record AuthResponse(
    Guid UserAccountId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record LoginRequest(
    string Login,
    string Password);

public sealed record RefreshSessionRequest(string RefreshToken);

public sealed record RegisterRequest(
    string Login,
    string Password);

public sealed record RevokeSessionRequest(string RefreshToken);
