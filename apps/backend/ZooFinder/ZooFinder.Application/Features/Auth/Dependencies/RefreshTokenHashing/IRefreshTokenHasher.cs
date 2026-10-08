namespace ZooFinder.Application.Features.Auth.Dependencies.RefreshTokenHashing;

public interface IRefreshTokenHasher
{
    string HashToken(string refreshToken);
}
