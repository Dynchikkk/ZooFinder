using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZooFinder.Api.Configuration;
using ZooFinder.Application.Modules.Auth.DataSources;
using ZooFinder.Application.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Application.Modules.Auth.Dependencies.PasswordHashing;
using ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenGeneration;
using ZooFinder.Application.Modules.Auth.Dependencies.RefreshTokenHashing;
using ZooFinder.Application.Modules.Auth.Modules.Authentication.Services.Auth;
using ZooFinder.Application.Modules.Auth.Settings;
using ZooFinder.Infrastructure.Modules.Auth.DataSources;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.AccessToken;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.PasswordHashing;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.RefreshTokenGeneration;
using ZooFinder.Infrastructure.Modules.Auth.Dependencies.RefreshTokenHashing;

namespace ZooFinder.Api.DependencyInjection;

internal static class AuthServiceCollectionExtensions
{
    private const string AuthSection = "Auth";
    private const string JwtSection = "Jwt";

    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.AddOptions<AuthSettings>()
            .Bind(configuration.GetRequiredSection(AuthSection))
            .Validate(settings => settings.RefreshSessionLifetime > TimeSpan.Zero,
                "Auth:RefreshSessionLifetime must be positive.")
            .ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<AuthSettings>>().Value);
        services.AddSingleton(ConfigurationReader.GetSettings<JwtAccessTokenSettings>(configuration, JwtSection));

        // Providers
        services.AddSingleton<IAccessTokenProvider, JwtAccessTokenProvider>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, CryptographicRefreshTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();

        // Data sources
        services.AddScoped<IAuthDataSource, AuthDataSource>();

        // Services
        services.AddScoped<IAuthService, AuthService>();

        // Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtAccessTokenSettings>(ConfigureJwtBearer);
        services.AddAuthorization();

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, JwtAccessTokenSettings settings)
    {
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKeyBase64)),
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
        options.Events.OnTokenValidated = ValidateTokenSubject;
    }

    private static Task ValidateTokenSubject(TokenValidatedContext context)
    {
        string? subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(subject, out Guid userAccountId) || userAccountId == Guid.Empty)
        {
            context.Fail("Access token subject is invalid.");
        }

        return Task.CompletedTask;
    }
}
