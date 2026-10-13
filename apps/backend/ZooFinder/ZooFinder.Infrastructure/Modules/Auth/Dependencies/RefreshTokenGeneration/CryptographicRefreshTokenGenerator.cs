using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenGeneration;

namespace ZooFinder.Infrastructure.Modules.Auth.Dependencies.RefreshTokenGeneration;

public sealed class CryptographicRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string GenerateToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(tokenBytes);
    }
}
