using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Application.Features.Parks.Connections.Services.ParkConnections;

public interface IParkConnectionService
{
    Task<ParkConnectionResponse> GetAsync(Guid requestId, CancellationToken cancellationToken);
    Task<OffsetPageResponse<ParkConnectionResponse>> SearchAsync(Guid parkId,
        OffsetPageRequest pageRequest, CancellationToken cancellationToken);
    Task<ParkConnectionResponse> CreateAsync(CreateParkConnectionRequest request, CancellationToken cancellationToken);
    Task<ParkConnectionResponse> ApproveAsync(ApproveParkConnectionRequest request, CancellationToken cancellationToken);
    Task<ParkConnectionResponse> ConfirmPaymentAsync(Guid requestId, CancellationToken cancellationToken);
    Task<ParkConnectionResponse> CloseAsync(CloseParkConnectionRequest request, CancellationToken cancellationToken);
}
