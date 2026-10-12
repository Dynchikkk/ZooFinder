using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Modules.Auth.Dependencies.AccessToken;

public interface IAccessTokenProvider
{
    string GenerateToken(UserAccount userAccount);
}
