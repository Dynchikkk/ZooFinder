using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Auth.Contracts;
using ZooFinder.Application.Features.Auth.Interfaces;
using ZooFinder.Application.Features.Auth.Settings;
using ZooFinder.Application.Features.Auth.Validators;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthSettings _authSettings;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IAuthRepository authRepository,
        IAccessTokenProvider accessTokenProvider,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IPasswordHasher passwordHasher,
        AuthSettings authSettings,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(authRepository);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenHasher);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(authSettings);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (authSettings.RefreshSessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(authSettings),
                "Refresh session lifetime must be greater than zero.");
        }

        _authRepository = authRepository;
        _accessTokenProvider = accessTokenProvider;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _passwordHasher = passwordHasher;
        _authSettings = authSettings;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string login = NormalizeLogin(request.Login);
        string password = request.Password ?? string.Empty;

        AuthValidator.ValidatePassword(password);

        if (await _authRepository.IsLoginTakenAsync(login, cancellationToken))
        {
            throw new ConflictException("Login is already taken.");
        }

        string passwordHash = HashPassword(password);

        DateTime currentTime = GetCurrentTime();
        (string refreshToken, string refreshTokenHash) = GenerateRefreshToken();

        var userAccount = new UserAccount
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = passwordHash,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        var userProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccount.Id,
            UserAccount = userAccount,
            DisplayName = login,
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        var refreshSession = new UserRefreshSession
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccount.Id,
            UserAccount = userAccount,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAtUtc = currentTime.Add(_authSettings.RefreshSessionLifetime),
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        userAccount.UserProfile = userProfile;
        userAccount.UserRefreshSessions.Add(refreshSession);

        string accessToken = GenerateAccessToken(userAccount);

        await _authRepository.AddUserAccountAsync(userAccount, cancellationToken);

        return new AuthResponse(
            userAccount.Id,
            accessToken,
            refreshToken,
            refreshSession.ExpiresAtUtc);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string login = NormalizeLogin(request.Login);
        string password = request.Password ?? string.Empty;
        AuthValidator.ValidatePassword(password);

        UserAccount userAccount = await _authRepository.GetUserAccountByLoginAsync(
            login,
            cancellationToken)
            ?? throw new UnauthorizedException("Login or password is invalid.");

        if (userAccount.IsDeleted ||
            userAccount.Status != UserStatus.Active ||
            string.IsNullOrWhiteSpace(userAccount.PasswordHash) ||
            !_passwordHasher.VerifyPassword(password, userAccount.PasswordHash))
        {
            throw new UnauthorizedException("Login or password is invalid.");
        }

        DateTime currentTime = GetCurrentTime();
        (string refreshToken, string refreshTokenHash) = GenerateRefreshToken();

        var refreshSession = new UserRefreshSession
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccount.Id,
            UserAccount = userAccount,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAtUtc = currentTime.Add(_authSettings.RefreshSessionLifetime),
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        string accessToken = GenerateAccessToken(userAccount);

        await _authRepository.AddRefreshSessionAsync(refreshSession, cancellationToken);

        return new AuthResponse(
            userAccount.Id,
            accessToken,
            refreshToken,
            refreshSession.ExpiresAtUtc);
    }

    public async Task<AuthResponse> RefreshSessionAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string currentRefreshToken = NormalizeRefreshToken(request.RefreshToken);
        string currentRefreshTokenHash = HashRefreshToken(currentRefreshToken);

        UserRefreshSession refreshSession = await _authRepository.GetRefreshSessionWithUserAccountByTokenHashAsync(
            currentRefreshTokenHash,
            cancellationToken)
            ?? throw new UnauthorizedException("Refresh token is invalid.");

        DateTime currentTime = GetCurrentTime();
        UserAccount userAccount = refreshSession.UserAccount
            ?? throw new InvalidOperationException("Refresh session was loaded without its user account.");

        ValidateRefreshSession(refreshSession, currentTime);
        ValidateUserAccount(userAccount);

        (string newRefreshToken, string newRefreshTokenHash) = GenerateRefreshToken();
        string accessToken = GenerateAccessToken(userAccount);

        refreshSession.RefreshTokenHash = newRefreshTokenHash;
        refreshSession.ExpiresAtUtc = currentTime.Add(_authSettings.RefreshSessionLifetime);
        refreshSession.LastUsedAtUtc = currentTime;
        refreshSession.UpdatedAtUtc = currentTime;

        await _authRepository.UpdateRefreshSessionAsync(refreshSession, cancellationToken);

        return new AuthResponse(
            userAccount.Id,
            accessToken,
            newRefreshToken,
            refreshSession.ExpiresAtUtc);
    }

    public async Task RevokeSessionAsync(
        RevokeSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string refreshToken = NormalizeRefreshToken(request.RefreshToken);
        string refreshTokenHash = HashRefreshToken(refreshToken);

        UserRefreshSession refreshSession = await _authRepository.GetRefreshSessionWithUserAccountByTokenHashAsync(
            refreshTokenHash,
            cancellationToken)
            ?? throw new UnauthorizedException("Refresh token is invalid.");

        if (refreshSession.RevokedAtUtc != null)
        {
            return;
        }

        DateTime currentTime = GetCurrentTime();
        refreshSession.RevokedAtUtc = currentTime;
        refreshSession.UpdatedAtUtc = currentTime;

        await _authRepository.UpdateRefreshSessionAsync(refreshSession, cancellationToken);
    }

    public Task RevokeAllSessionsAsync(
        Guid currentUserAccountId,
        CancellationToken cancellationToken)
    {
        AuthValidator.ValidateUserAccountId(currentUserAccountId);

        return _authRepository.RevokeAllRefreshSessionsAsync(
            currentUserAccountId,
            GetCurrentTime(),
            cancellationToken);
    }

    private string NormalizeRefreshToken(string? refreshToken)
    {
        string normalizedRefreshToken = refreshToken?.Trim() ?? string.Empty;
        AuthValidator.ValidateRefreshToken(normalizedRefreshToken);
        return normalizedRefreshToken;
    }

    private static string NormalizeLogin(string? login)
    {
        string normalizedLogin = login?.Trim().ToLowerInvariant() ?? string.Empty;
        AuthValidator.ValidateLogin(normalizedLogin);
        return normalizedLogin;
    }

    private string HashPassword(string password)
    {
        string passwordHash = _passwordHasher.HashPassword(password);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new InvalidOperationException("Password hasher returned an empty hash.");
        }

        return passwordHash;
    }

    private (string Token, string Hash) GenerateRefreshToken()
    {
        string refreshToken = _refreshTokenGenerator.GenerateToken();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException("Refresh token generator returned an empty token.");
        }

        string refreshTokenHash = HashRefreshToken(refreshToken);
        return (refreshToken, refreshTokenHash);
    }

    private string HashRefreshToken(string refreshToken)
    {
        string refreshTokenHash = _refreshTokenHasher.HashToken(refreshToken);

        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            throw new InvalidOperationException("Refresh token hasher returned an empty hash.");
        }

        return refreshTokenHash;
    }

    private string GenerateAccessToken(UserAccount userAccount)
    {
        string accessToken = _accessTokenProvider.GenerateToken(userAccount);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Access token provider returned an empty token.");
        }

        return accessToken;
    }

    private static void ValidateRefreshSession(
        UserRefreshSession refreshSession,
        DateTime currentTime)
    {
        if (refreshSession.IsDeleted ||
            refreshSession.RevokedAtUtc != null ||
            refreshSession.ExpiresAtUtc <= currentTime)
        {
            throw new UnauthorizedException("Refresh session is no longer active.");
        }
    }

    private static void ValidateUserAccount(UserAccount userAccount)
    {
        if (userAccount.IsDeleted || userAccount.Status != UserStatus.Active)
        {
            throw new UnauthorizedException("User account is not active.");
        }
    }

    private DateTime GetCurrentTime()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }
}
