using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Catalog.DataSources;
using ZooFinder.Application.Features.Parks.Catalog.Validators;
using ZooFinder.Application.Features.Parks.Connections.Constants;
using ZooFinder.Application.Features.Parks.Connections.DataSources;
using ZooFinder.Application.Features.Parks.Connections.Validators;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Connections.Services.ParkConnections;

public sealed class ParkConnectionService : IParkConnectionService
{
    private readonly IParkCatalogDataSource _parks;
    private readonly IParkConnectionDataSource _dataSource;
    private readonly TimeProvider _timeProvider;

    public ParkConnectionService(IParkCatalogDataSource parks, IParkConnectionDataSource dataSource, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(parks);
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _parks = parks;
        _dataSource = dataSource;
        _timeProvider = timeProvider;
    }

    public async Task<ParkConnectionResponse> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await GetRequestAsync(requestId, cancellationToken);
        return new ParkConnectionResponse(request.Id, request.ParkId, request.Status, request.ContactName,
            request.Contact, request.AgreedAmount, request.Currency, request.PaidAtUtc, request.Comment);
    }

    public async Task<OffsetPageResponse<ParkConnectionResponse>> SearchAsync(
        Guid parkId,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pageRequest);
        await ValidateParkAsync(parkId, cancellationToken);
        ParkValidator.ValidatePage(pageRequest.Page, pageRequest.PageSize);
        var page = await _dataSource.SearchAsync(parkId, pageRequest, cancellationToken);
        return new OffsetPageResponse<ParkConnectionResponse>(page.Items
            .Select(request =>
                new ParkConnectionResponse(request.Id, request.ParkId, request.Status, request.ContactName,
                    request.Contact, request.AgreedAmount, request.Currency, request.PaidAtUtc, request.Comment))
            .ToArray(),
            page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ParkConnectionResponse> CreateAsync(
        CreateParkConnectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateParkAsync(request.ParkId, cancellationToken);
        var connection = new ParkConnectionRequest
        {
            Id = Guid.NewGuid(),
            ParkId = request.ParkId,
            ContactName = ParkConnectionValidator.NormalizeText(
                request.ContactName, ParkConnectionConstraints.MaximumContactNameLength),
            Contact = ParkConnectionValidator.NormalizeText(request.Contact, ParkConnectionConstraints.MaximumContactLength),
            Comment = ParkConnectionValidator.NormalizeText(request.Comment, ParkConnectionConstraints.MaximumCommentLength)
        };

        if (!await _dataSource.TryAddAsync(connection, cancellationToken))
        {
            throw new ConflictException("Park already has an unfinished connection request.");
        }

        return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
            connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
    }

    public async Task<ParkConnectionResponse> ApproveAsync(
        ApproveParkConnectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string currency = request.Currency?.Trim().ToUpperInvariant() ?? string.Empty;
        ParkConnectionValidator.ValidatePrice(request.Amount, currency);
        string? comment = ParkConnectionValidator.NormalizeText(request.Comment, ParkConnectionConstraints.MaximumCommentLength);
        var connection = await GetRequestAsync(request.ConnectionRequestId, cancellationToken);
        if (connection.Status == ParkConnectionStatus.AwaitingPayment &&
            connection.AgreedAmount == request.Amount && connection.Currency == currency && connection.Comment == comment)
        {
            return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
                connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
        }

        if (connection.Status != ParkConnectionStatus.Submitted)
        {
            throw new ConflictException("Only a submitted request can be approved.");
        }

        connection.Status = ParkConnectionStatus.AwaitingPayment;
        connection.AgreedAmount = request.Amount;
        connection.Currency = currency;
        connection.Comment = comment;
        if (!await _dataSource.TryUpdateAsync(connection, ParkConnectionStatus.Submitted, cancellationToken))
        {
            throw new ConflictException("Connection request changed.");
        }

        return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
            connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
    }

    public async Task<ParkConnectionResponse> ConfirmPaymentAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var connection = await GetRequestAsync(requestId, cancellationToken);
        if (connection.Status == ParkConnectionStatus.Activated)
        {
            return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
                connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
        }

        if (connection.Status != ParkConnectionStatus.AwaitingPayment ||
            !connection.AgreedAmount.HasValue || string.IsNullOrWhiteSpace(connection.Currency))
        {
            throw new ConflictException("Payment can only be confirmed after approval.");
        }

        connection.Status = ParkConnectionStatus.Activated;
        connection.PaidAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await _dataSource.TryUpdateAsync(connection, ParkConnectionStatus.AwaitingPayment, cancellationToken))
        {
            throw new ConflictException("Connection request changed.");
        }

        return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
            connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
    }

    public async Task<ParkConnectionResponse> CloseAsync(CloseParkConnectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Status is not (ParkConnectionStatus.Rejected or ParkConnectionStatus.Cancelled))
        {
            throw new RequestValidationException("Closing status must be Rejected or Cancelled.");
        }

        string? comment = ParkConnectionValidator.NormalizeText(request.Comment, ParkConnectionConstraints.MaximumCommentLength);
        var connection = await GetRequestAsync(request.ConnectionRequestId, cancellationToken);
        if (connection.Status == request.Status)
        {
            return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
                connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
        }

        if (connection.Status is not (ParkConnectionStatus.Submitted or ParkConnectionStatus.AwaitingPayment))
        {
            throw new ConflictException("Completed requests cannot be closed again.");
        }

        var expected = connection.Status;
        connection.Status = request.Status;
        connection.Comment = comment;
        if (!await _dataSource.TryUpdateAsync(connection, expected, cancellationToken))
        {
            throw new ConflictException("Connection request changed.");
        }

        return new ParkConnectionResponse(connection.Id, connection.ParkId, connection.Status, connection.ContactName,
            connection.Contact, connection.AgreedAmount, connection.Currency, connection.PaidAtUtc, connection.Comment);
    }

    private async Task<ParkConnectionRequest> GetRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        ParkConnectionValidator.ValidateId(id);
        return await _dataSource.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Connection request was not found.");
    }

    private async Task ValidateParkAsync(Guid id, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(id);
        _ = await _parks.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Park was not found.");
    }
}
