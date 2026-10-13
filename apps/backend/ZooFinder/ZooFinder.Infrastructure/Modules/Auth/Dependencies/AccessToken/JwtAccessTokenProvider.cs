using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ZooFinder.Application.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Domain.Users;

namespace ZooFinder.Infrastructure.Modules.Auth.Dependencies.AccessToken;

public sealed class JwtAccessTokenProvider : IAccessTokenProvider
{
    private readonly JwtAccessTokenSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;

    public JwtAccessTokenProvider(JwtAccessTokenSettings settings, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        byte[] signingKey = JwtAccessTokenSettingsValidator.ValidateAndGetSigningKey(settings);

        _settings = settings;
        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256);
    }

    public string GenerateToken(UserAccount userAccount)
    {
        ArgumentNullException.ThrowIfNull(userAccount);

        if (userAccount.Id == Guid.Empty || userAccount.IsDeleted || userAccount.Status != UserStatus.Active)
        {
            throw new InvalidOperationException("Access tokens require an active user account with a non-empty ID.");
        }

        if (userAccount.Role != UserRole.User && userAccount.Role != UserRole.Moderator &&
            userAccount.Role != UserRole.Administrator)
        {
            throw new InvalidOperationException("Access tokens require a valid user role.");
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new Claim("role", userAccount.Role.ToString())
        ];

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(_settings.Lifetime).UtcDateTime,
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
