namespace ZooFinder.Application.Features.Auth.Dependencies.PasswordHashing;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
