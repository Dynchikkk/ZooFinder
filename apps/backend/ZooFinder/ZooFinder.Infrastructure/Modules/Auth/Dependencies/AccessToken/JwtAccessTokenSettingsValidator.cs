namespace ZooFinder.Infrastructure.Modules.Auth.Dependencies.AccessToken;

internal static class JwtAccessTokenSettingsValidator
{
    public static byte[] ValidateAndGetSigningKey(JwtAccessTokenSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new ArgumentException("JWT issuer and audience must not be empty.", nameof(settings));
        }

        if (settings.Lifetime < TimeSpan.FromSeconds(1) || settings.Lifetime > TimeSpan.FromHours(1))
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "JWT lifetime must be between one second and one hour.");
        }

        if (string.IsNullOrWhiteSpace(settings.SigningKeyBase64))
        {
            throw new ArgumentException("JWT signing key must be configured as Base64.", nameof(settings));
        }

        byte[] signingKey;

        try
        {
            signingKey = Convert.FromBase64String(settings.SigningKeyBase64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("JWT signing key must be valid Base64.", nameof(settings));
        }

        if (signingKey.Length < 32)
        {
            throw new ArgumentException("JWT signing key must contain at least 32 random bytes.", nameof(settings));
        }

        return signingKey;
    }
}
