using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Application.Modules.Parks.Modules.Animals.Services.ParkAnimals;

public interface IParkAnimalService
{
    Task<ParkAnimalResponse> GetAsync(
        Guid parkAnimalId,
        CancellationToken cancellationToken);

    Task<OffsetPageResponse<ParkAnimalResponse>> SearchAsync(
        ParkAnimalSearchRequest request,
        CancellationToken cancellationToken);

    Task<ParkAnimalResponse> AddAsync(
        AddParkAnimalRequest request,
        CancellationToken cancellationToken);

    Task<ParkAnimalResponse> UpdateAsync(
        UpdateParkAnimalRequest request,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid parkAnimalId,
        CancellationToken cancellationToken);
}
