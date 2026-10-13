using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ZooFinder.Application.Modules.Auth.Dependencies.PasswordHashing;

namespace ZooFinder.Infrastructure.Modules.Auth.Dependencies.PasswordHashing;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly object _subject = new();
    private readonly PasswordHasher<object> _passwordHasher;

    public IdentityPasswordHasher()
    {
        var options = new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = 220_000
        };

        _passwordHasher = new PasswordHasher<object>(Options.Create(options));
    }

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _passwordHasher.HashPassword(_subject, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(_subject, passwordHash, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
