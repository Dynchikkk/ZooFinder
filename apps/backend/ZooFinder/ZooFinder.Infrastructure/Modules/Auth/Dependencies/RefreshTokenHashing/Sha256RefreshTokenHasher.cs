using System.Security.Cryptography;
using System.Text;
using ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenHashing;

namespace ZooFinder.Infrastructure.Modules.Auth.Dependencies.RefreshTokenHashing;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string HashToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        byte[] tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        return Convert.ToHexString(SHA256.HashData(tokenBytes));
    }
}
