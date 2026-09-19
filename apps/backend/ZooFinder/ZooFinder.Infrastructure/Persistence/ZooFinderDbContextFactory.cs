using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZooFinder.Infrastructure.Persistence;

public sealed class ZooFinderDbContextFactory : IDesignTimeDbContextFactory<ZooFinderDbContext>
{
    public ZooFinderDbContext CreateDbContext(string[] args)
    {
        // The fallback supports generating migrations without a running server.
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ZooFinder")
            ?? "Server=localhost;Database=ZooFinder;Integrated Security=true;TrustServerCertificate=true";
        var options = new DbContextOptionsBuilder<ZooFinderDbContext>()
            .UseSqlServer(connectionString).Options;
        return new ZooFinderDbContext(options, TimeProvider.System);
    }
}

