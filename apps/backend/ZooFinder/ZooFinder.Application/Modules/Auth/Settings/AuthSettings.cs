namespace ZooFinder.Application.Modules.Auth.Settings;

public sealed class AuthSettings
{
    public TimeSpan RefreshSessionLifetime { get; init; } = TimeSpan.FromDays(30);
}
