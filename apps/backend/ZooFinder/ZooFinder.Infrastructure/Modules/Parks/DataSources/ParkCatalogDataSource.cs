using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Parks.DataSources;

public sealed class ParkCatalogDataSource : IParkCatalogDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public ParkCatalogDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<Park?> GetAsync(Guid parkId, CancellationToken cancellationToken)
    {
        return _dbContext.Parks
            .AsNoTracking()
            .SingleOrDefaultAsync(park => park.Id == parkId, cancellationToken);
    }

    public async Task<OffsetPageResponse<Park>> SearchAsync(
        string searchTerm,
        bool includeSuspended,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Parks.AsNoTracking();

        if (!includeSuspended)
        {
            query = query.Where(park => park.Status == ParkStatus.Active);
        }

        if (searchTerm.Length > 0)
        {
            query = query.Where(park => park.Name.Contains(searchTerm));
        }

        long count = await query.LongCountAsync(cancellationToken);
        var parks = await query
            .OrderBy(park => park.Name)
            .ThenBy(park => park.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new OffsetPageResponse<Park>(parks, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<bool> TryAddAsync(Park park, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(park);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        if (await _dbContext.Parks
            .IgnoreQueryFilters()
            .AnyAsync(existing => existing.Slug == park.Slug, cancellationToken))
        {
            return false;
        }

        _dbContext.Entry(park).State = EntityState.Added;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryUpdateAsync(Park park, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(park);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        if (await _dbContext.Parks
            .IgnoreQueryFilters()
            .AnyAsync(existing => existing.Id != park.Id && existing.Slug == park.Slug, cancellationToken))
        {
            return false;
        }

        Park? existingPark = await _dbContext.Parks.SingleOrDefaultAsync(existing => existing.Id == park.Id, cancellationToken);

        if (existingPark == null)
        {
            return false;
        }

        existingPark.Name = park.Name;
        existingPark.Slug = park.Slug;
        existingPark.Description = park.Description;
        existingPark.Address = park.Address;
        existingPark.Status = park.Status;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            park.UpdatedAtUtc = existingPark.UpdatedAtUtc;
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
