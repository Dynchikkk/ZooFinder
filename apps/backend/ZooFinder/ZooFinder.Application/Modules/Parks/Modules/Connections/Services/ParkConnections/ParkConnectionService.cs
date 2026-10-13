using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Modules.Parks.Extensions;
using ZooFinder.Application.Modules.Parks.Modules.Connections.Constants;
using ZooFinder.Application.Modules.Parks.Modules.Connections.DataSources;
using ZooFinder.Application.Modules.Parks.Modules.Connections.Validators;
using ZooFinder.Application.Modules.Parks.Validators;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Parks.Modules.Connections.Services.ParkConnections;

public sealed class ParkConnectionService : IParkConnectionService
{
    private readonly IParkCatalogDataSource _parkCatalogDataSource;
    private readonly IParkConnectionDataSource _parkConnectionDataSource;
    private readonly TimeProvider _timeProvider;

    public ParkConnectionService(
        IParkCatalogDataSource parkCatalogDataSource,
        IParkConnectionDataSource parkConnectionDataSource,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(parkCatalogDataSource);
        ArgumentNullException.ThrowIfNull(parkConnectionDataSource);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _parkCatalogDataSource = parkCatalogDataSource;
        _parkConnectionDataSource = parkConnectionDataSource;
        _timeProvider = timeProvider;
    }

    public async Task<ParkConnectionResponse> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        ParkConnectionRequest connection = await GetConnectionAsync(requestId, cancellationToken);
        return ToResponse(connection);
    }

    public async Task<OffsetPageResponse<ParkConnectionResponse>> SearchAsync(
        Guid parkId,
        OffsetPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pageRequest);
        await ValidateParkAsync(parkId, cancellationToken);
        ParkValidator.ValidatePage(pageRequest.Page, pageRequest.PageSize);

        var page = await _parkConnectionDataSource.SearchAsync(parkId, pageRequest, cancellationToken);
        var connections = page.Items
            .Select(ToResponse)
            .ToArray();

        return new OffsetPageResponse<ParkConnectionResponse>(connections, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ParkConnectionResponse> CreateAsync(
        CreateParkConnectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateParkAsync(request.ParkId, cancellationToken);

        string? contactName = request.ContactName.NormalizeOptionalText();
        string? contact = request.Contact.NormalizeOptionalText();
        string? comment = request.Comment.NormalizeOptionalText();

        ParkConnectionValidator.ValidateText(contactName, ParkConnectionConstraints.MaximumContactNameLength);
        ParkConnectionValidator.ValidateText(contact, ParkConnectionConstraints.MaximumContactLength);
        ParkConnectionValidator.ValidateText(comment, ParkConnectionConstraints.MaximumCommentLength);

        var connection = new ParkConnectionRequest
        {
            Id = Guid.NewGuid(),
            ParkId = request.ParkId,
            ContactName = contactName,
            Contact = contact,
            Comment = comment
        };

        bool wasAdded = await _parkConnectionDataSource.TryAddAsync(connection, cancellationToken);

        if (!wasAdded)
        {
            throw new ConflictException("Park already has an unfinished connection request.");
        }

        return ToResponse(connection);
    }

    public async Task<ParkConnectionResponse> ApproveAsync(
        ApproveParkConnectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string currency = request.Currency.NormalizeText().ToUpperInvariant();
        ParkConnectionValidator.ValidatePrice(request.Amount, currency);

        string? comment = request.Comment.NormalizeOptionalText();
        ParkConnectionValidator.ValidateText(comment, ParkConnectionConstraints.MaximumCommentLength);

        ParkConnectionRequest connection = await GetConnectionAsync(request.ConnectionRequestId, cancellationToken);

        if (connection.Status == ParkConnectionStatus.AwaitingPayment &&
            connection.AgreedAmount == request.Amount && connection.Currency == currency && connection.Comment == comment)
        {
            return ToResponse(connection);
        }

        if (connection.Status != ParkConnectionStatus.Submitted)
        {
            throw new ConflictException("Only a submitted request can be approved.");
        }

        connection.Status = ParkConnectionStatus.AwaitingPayment;
        connection.AgreedAmount = request.Amount;
        connection.Currency = currency;
        connection.Comment = comment;

        await UpdateConnectionAsync(connection, ParkConnectionStatus.Submitted, cancellationToken);
        return ToResponse(connection);
    }

    public async Task<ParkConnectionResponse> ConfirmPaymentAsync(Guid requestId, CancellationToken cancellationToken)
    {
        ParkConnectionRequest connection = await GetConnectionAsync(requestId, cancellationToken);

        if (connection.Status == ParkConnectionStatus.Activated)
        {
            return ToResponse(connection);
        }

        if (connection.Status != ParkConnectionStatus.AwaitingPayment ||
            !connection.AgreedAmount.HasValue || string.IsNullOrWhiteSpace(connection.Currency))
        {
            throw new ConflictException("Payment can only be confirmed after approval.");
        }

        connection.Status = ParkConnectionStatus.Activated;
        connection.PaidAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await UpdateConnectionAsync(connection, ParkConnectionStatus.AwaitingPayment, cancellationToken);
        return ToResponse(connection);
    }

    public async Task<ParkConnectionResponse> CloseAsync(CloseParkConnectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Status != ParkConnectionStatus.Rejected && request.Status != ParkConnectionStatus.Cancelled)
        {
            throw new RequestValidationException("Closing status must be Rejected or Cancelled.");
        }

        string? comment = request.Comment.NormalizeOptionalText();
        ParkConnectionValidator.ValidateText(comment, ParkConnectionConstraints.MaximumCommentLength);

        ParkConnectionRequest connection = await GetConnectionAsync(request.ConnectionRequestId, cancellationToken);

        if (connection.Status == request.Status)
        {
            return ToResponse(connection);
        }

        if (connection.Status != ParkConnectionStatus.Submitted && connection.Status != ParkConnectionStatus.AwaitingPayment)
        {
            throw new ConflictException("Completed requests cannot be closed again.");
        }

        ParkConnectionStatus expectedStatus = connection.Status;
        connection.Status = request.Status;
        connection.Comment = comment;

        await UpdateConnectionAsync(connection, expectedStatus, cancellationToken);
        return ToResponse(connection);
    }

    private async Task<ParkConnectionRequest> GetConnectionAsync(Guid requestId, CancellationToken cancellationToken)
    {
        ParkConnectionValidator.ValidateId(requestId);

        return await _parkConnectionDataSource.GetAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("Connection request was not found.");
    }

    private async Task UpdateConnectionAsync(
        ParkConnectionRequest connection,
        ParkConnectionStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        bool wasUpdated = await _parkConnectionDataSource.TryUpdateAsync(connection, expectedStatus, cancellationToken);

        if (!wasUpdated)
        {
            throw new ConflictException("Connection request changed.");
        }
    }

    private async Task ValidateParkAsync(Guid parkId, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);

        if (await _parkCatalogDataSource.GetAsync(parkId, cancellationToken) == null)
        {
            throw new NotFoundException("Park was not found.");
        }
    }

    private static ParkConnectionResponse ToResponse(ParkConnectionRequest connection)
    {
        return new ParkConnectionResponse(
            Id: connection.Id,
            ParkId: connection.ParkId,
            Status: connection.Status,
            ContactName: connection.ContactName,
            Contact: connection.Contact,
            AgreedAmount: connection.AgreedAmount,
            Currency: connection.Currency,
            PaidAtUtc: connection.PaidAtUtc,
            Comment: connection.Comment);
    }
}
