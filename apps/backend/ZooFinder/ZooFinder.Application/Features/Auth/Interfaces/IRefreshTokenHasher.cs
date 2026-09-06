namespace ZooFinder.Application.Features.Auth.Interfaces;

public interface IRefreshTokenHasher
{
    string HashToken(string refreshToken);
}
