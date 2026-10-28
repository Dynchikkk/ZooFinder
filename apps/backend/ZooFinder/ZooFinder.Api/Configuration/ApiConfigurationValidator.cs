using Microsoft.Data.SqlClient;
using ZooFinder.Application.Modules.Auth.Settings;

namespace ZooFinder.Api.Configuration;

internal static class ApiConfigurationValidator
{
    public static void ValidateConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:ZooFinder is required.");
        }

        var settings = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(settings.DataSource) || string.IsNullOrWhiteSpace(settings.InitialCatalog))
        {
            throw new InvalidOperationException("ZooFinder connection string must specify a server and database.");
        }
    }

    public static void ValidateAuthSettings(AuthSettings settings)
    {
        if (settings.RefreshSessionLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Refresh session lifetime must be positive.");
        }
    }
}
