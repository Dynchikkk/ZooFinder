using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Auth.Interfaces;

public interface IAccessTokenProvider
{
    string GenerateToken(UserAccount userAccount);
}
