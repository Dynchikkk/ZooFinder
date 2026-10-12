namespace ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenHashing;

public interface IRefreshTokenHasher
{
    string HashToken(string refreshToken);
}
