using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Connections.DataSources;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Infrastructure.Persistence.DataSources.Features.Parks.Connections;

public sealed class ParkConnectionDataSource(ZooFinderDbContext dbContext, TimeProvider timeProvider) : IParkConnectionDataSource
{
    public Task<ParkConnectionRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return dbContext.ParkConnectionRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);
    }

    public async Task<OffsetPageResponse<ParkConnectionRequest>> SearchAsync(
        Guid parkId,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ParkConnectionRequests
            .AsNoTracking()
            .Where(request => request.ParkId == parkId);
        long count = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(request => request.CreatedAtUtc)
            .ThenBy(request => request.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);
        return new OffsetPageResponse<ParkConnectionRequest>(items, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<bool> TryAddAsync(ParkConnectionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        if (!await dbContext.Parks.AnyAsync(park => park.Id == request.ParkId, cancellationToken) ||
            await dbContext.ParkConnectionRequests.AnyAsync(
                existing => existing.ParkId == request.ParkId &&
                    (existing.Status == ParkConnectionStatus.Submitted ||
                        existing.Status == ParkConnectionStatus.AwaitingPayment),
                cancellationToken))
        {
            return false;
        }

        dbContext.Entry(request).State = EntityState.Added;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            dbContext.Entry(request).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<bool> TryUpdateAsync(
        ParkConnectionRequest request,
        ParkConnectionStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int count = await dbContext.ParkConnectionRequests
            .Where(existing => existing.Id == request.Id && existing.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(existing => existing.Status, request.Status)
                .SetProperty(existing => existing.AgreedAmount, request.AgreedAmount)
                .SetProperty(existing => existing.Currency, request.Currency)
                .SetProperty(existing => existing.PaidAtUtc, request.PaidAtUtc)
                .SetProperty(existing => existing.Comment, request.Comment)
                .SetProperty(existing => existing.UpdatedAtUtc, now), cancellationToken);
        return count == 1;
    }
}
