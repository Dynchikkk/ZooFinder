using ZooFinder.Application.Common.ErrorHandling.Exceptions;

namespace ZooFinder.Application.Features.Auth.Validators;

public static class AuthValidator
{
    private const int MaximumLoginLength = 100;
    private const int MaximumPasswordLength = 200;
    private const int MaximumRefreshTokenLength = 2_000;

    public static void ValidateLogin(string login)
    {
        if (login.Length == 0 || login.Length > MaximumLoginLength)
        {
            throw new RequestValidationException(
                $"Login length must be between 1 and {MaximumLoginLength} characters.");
        }
    }

    public static void ValidatePassword(string password)
    {
        if (password.Length == 0 || password.Length > MaximumPasswordLength)
        {
            throw new RequestValidationException(
                $"Password length must be between 1 and {MaximumPasswordLength} characters.");
        }
    }

    public static void ValidateRefreshToken(string refreshToken)
    {
        if (refreshToken.Length == 0 || refreshToken.Length > MaximumRefreshTokenLength)
        {
            throw new RequestValidationException(
                $"Refresh token length must be between 1 and {MaximumRefreshTokenLength} characters.");
        }
    }

    public static void ValidateUserAccountId(Guid userAccountId)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new RequestValidationException("User account ID must not be empty.");
        }
    }
}
