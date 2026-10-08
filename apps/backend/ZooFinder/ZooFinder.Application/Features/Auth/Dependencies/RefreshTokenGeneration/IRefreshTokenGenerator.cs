namespace ZooFinder.Application.Features.Auth.Dependencies.RefreshTokenGeneration;

public interface IRefreshTokenGenerator
{
    string GenerateToken();
}
