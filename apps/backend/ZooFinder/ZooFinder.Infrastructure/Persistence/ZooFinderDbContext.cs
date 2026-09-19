using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.BaseEntities;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;
using ZooFinder.Infrastructure.Persistence.Configurations;

namespace ZooFinder.Infrastructure.Persistence;

public sealed class ZooFinderDbContext : DbContext
{
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<DiscussionRoom> DiscussionRooms => Set<DiscussionRoom>();
    public DbSet<DiscussionMessage> DiscussionMessages => Set<DiscussionMessage>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserRefreshSession> UserRefreshSessions => Set<UserRefreshSession>();

    private readonly TimeProvider _timeProvider;

    public ZooFinderDbContext(
        DbContextOptions<ZooFinderDbContext> options,
        TimeProvider timeProvider) : base(options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AnimalConfiguration());
        modelBuilder.ApplyConfiguration(new DiscussionRoomConfiguration());
        modelBuilder.ApplyConfiguration(new DiscussionMessageConfiguration());
        modelBuilder.ApplyConfiguration(new UserAccountConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new UserRefreshSessionConfiguration());

        // Store UTC values and restore their DateTime.Kind when reading.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value.ToUniversalTime(), 
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsKeyless)
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        ChangeTracker.DetectChanges();
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        var entries = ChangeTracker
            .Entries<IdEntity<Guid>>()
            .Where(entry => entry.State == EntityState.Added ||
                            entry.State == EntityState.Modified ||
                            entry.State == EntityState.Deleted)
            .ToArray();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = now;
                    entry.Entity.IsDeleted = false;
                    entry.Entity.DeletedAtUtc = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;

                    entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                    entry.Property(entity => entity.IsDeleted).IsModified = false;
                    entry.Property(entity => entity.UpdatedAtUtc).IsModified = true;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAtUtc = now;
                    entry.Entity.DeletedAtUtc = now;

                    entry.Property(entity => entity.IsDeleted).IsModified = true;
                    entry.Property(entity => entity.UpdatedAtUtc).IsModified = true;
                    entry.Property(entity => entity.DeletedAtUtc).IsModified = true;
                    break;
            }
        }
    }
}
