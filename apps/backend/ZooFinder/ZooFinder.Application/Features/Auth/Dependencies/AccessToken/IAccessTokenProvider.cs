using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Auth.Dependencies.AccessToken;

public interface IAccessTokenProvider
{
    string GenerateToken(UserAccount userAccount);
}
