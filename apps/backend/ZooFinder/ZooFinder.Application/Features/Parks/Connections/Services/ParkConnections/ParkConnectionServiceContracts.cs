using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Connections.Services.ParkConnections;

public sealed record ApproveParkConnectionRequest(
    Guid ConnectionRequestId,
    decimal Amount,
    string Currency,
    string? Comment = null);

public sealed record CloseParkConnectionRequest(
    Guid ConnectionRequestId,
    ParkConnectionStatus Status,
    string? Comment = null);

public sealed record CreateParkConnectionRequest(
    Guid ParkId,
    string? ContactName = null,
    string? Contact = null,
    string? Comment = null);

public sealed record ParkConnectionResponse(
    Guid Id,
    Guid ParkId,
    ParkConnectionStatus Status,
    string? ContactName,
    string? Contact,
    decimal? AgreedAmount,
    string? Currency,
    DateTime? PaidAtUtc,
    string? Comment);
