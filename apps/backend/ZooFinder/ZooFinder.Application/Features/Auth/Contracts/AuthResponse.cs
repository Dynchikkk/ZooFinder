namespace ZooFinder.Application.Features.Auth.Contracts;

public sealed record AuthResponse(
    Guid UserAccountId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
