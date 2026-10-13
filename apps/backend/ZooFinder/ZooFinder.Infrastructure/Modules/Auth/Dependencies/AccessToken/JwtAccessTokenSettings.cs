namespace ZooFinder.Infrastructure.Modules.Auth.Dependencies.AccessToken;

public sealed class JwtAccessTokenSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKeyBase64 { get; init; } = string.Empty;
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromMinutes(15);
}
