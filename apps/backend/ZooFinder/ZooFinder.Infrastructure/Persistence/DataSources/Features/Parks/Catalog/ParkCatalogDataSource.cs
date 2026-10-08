using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Catalog.DataSources;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Infrastructure.Persistence.DataSources.Features.Parks.Catalog;

public sealed class ParkCatalogDataSource(ZooFinderDbContext dbContext, TimeProvider timeProvider) : IParkCatalogDataSource
{
    public Task<Park?> GetAsync(Guid parkId, CancellationToken cancellationToken) =>
        dbContext.Parks.AsNoTracking().SingleOrDefaultAsync(park => park.Id == parkId, cancellationToken);

    public async Task<OffsetPageResponse<Park>> SearchAsync(string searchTerm, bool includeSuspended,
        OffsetPageRequest pageRequest, CancellationToken cancellationToken)
    {
        var query = dbContext.Parks.AsNoTracking();
        if (!includeSuspended) query = query.Where(park => park.Status == ParkStatus.Active);
        if (searchTerm.Length > 0) query = query.Where(park => park.Name.Contains(searchTerm));
        long count = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderBy(park => park.Name).ThenBy(park => park.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize).Take(pageRequest.PageSize).ToListAsync(cancellationToken);
        return new OffsetPageResponse<Park>(items, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<bool> TryAddAsync(Park park, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        if (await dbContext.Parks.IgnoreQueryFilters().AnyAsync(existing => existing.Slug == park.Slug, cancellationToken))
            return false;
        dbContext.Entry(park).State = EntityState.Added;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            dbContext.Entry(park).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<bool> TryUpdateAsync(Park park, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        if (await dbContext.Parks.IgnoreQueryFilters().AnyAsync(existing =>
                existing.Id != park.Id && existing.Slug == park.Slug, cancellationToken)) return false;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int count = await dbContext.Parks.Where(existing => existing.Id == park.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(existing => existing.Name, park.Name)
            .SetProperty(existing => existing.Slug, park.Slug)
            .SetProperty(existing => existing.Description, park.Description)
            .SetProperty(existing => existing.Address, park.Address)
            .SetProperty(existing => existing.Status, park.Status)
            .SetProperty(existing => existing.UpdatedAtUtc, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return count == 1;
    }
}
