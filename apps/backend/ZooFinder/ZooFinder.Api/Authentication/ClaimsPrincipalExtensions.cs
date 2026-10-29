using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Api.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserAccountId(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(subject, out Guid userAccountId) || userAccountId == Guid.Empty)
        {
            throw new UnauthorizedException("Access token subject is invalid.");
        }

        return userAccountId;
    }
}
