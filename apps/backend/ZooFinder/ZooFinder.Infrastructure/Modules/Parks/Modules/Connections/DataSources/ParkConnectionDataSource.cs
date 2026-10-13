using System.Data;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Parks.Modules.Connections.DataSources;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Parks.Modules.Connections.DataSources;

public sealed class ParkConnectionDataSource : IParkConnectionDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public ParkConnectionDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<ParkConnectionRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return _dbContext.ParkConnectionRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);
    }

    public async Task<OffsetPageResponse<ParkConnectionRequest>> SearchAsync(
        Guid parkId,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ParkConnectionRequests
            .AsNoTracking()
            .Where(request => request.ParkId == parkId);
        long count = await query.LongCountAsync(cancellationToken);
        var requests = await query
            .OrderByDescending(request => request.CreatedAtUtc)
            .ThenBy(request => request.Id)
            .Skip(pageRequest.Page * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new OffsetPageResponse<ParkConnectionRequest>(requests, pageRequest.Page, pageRequest.PageSize, count);
    }

    public async Task<bool> TryAddAsync(ParkConnectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        bool parkExists = await _dbContext.Parks.AnyAsync(park => park.Id == request.ParkId, cancellationToken);
        bool hasUnfinishedRequest = await _dbContext.ParkConnectionRequests.AnyAsync(
            existing => existing.ParkId == request.ParkId &&
                (existing.Status == ParkConnectionStatus.Submitted || existing.Status == ParkConnectionStatus.AwaitingPayment),
            cancellationToken);

        if (!parkExists || hasUnfinishedRequest)
        {
            return false;
        }

        _dbContext.Entry(request).State = EntityState.Added;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryUpdateAsync(
        ParkConnectionRequest request,
        ParkConnectionStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        ParkConnectionRequest? existingRequest = await _dbContext.ParkConnectionRequests
            .SingleOrDefaultAsync(existing => existing.Id == request.Id && existing.Status == expectedStatus, cancellationToken);

        if (existingRequest == null)
        {
            return false;
        }

        existingRequest.Status = request.Status;
        existingRequest.AgreedAmount = request.AgreedAmount;
        existingRequest.Currency = request.Currency;
        existingRequest.PaidAtUtc = request.PaidAtUtc;
        existingRequest.Comment = request.Comment;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            request.UpdatedAtUtc = existingRequest.UpdatedAtUtc;
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
