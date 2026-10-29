using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Api.DependencyInjection;

internal static class PersistenceServiceCollectionExtensions
{
    private const string ConnectionStringName = "ZooFinder";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException("ConnectionStrings:ZooFinder is required.");
        ValidateConnectionString(connectionString);

        // Context
        services.AddDbContext<ZooFinderDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }

    private static void ValidateConnectionString(string connectionString)
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
}
