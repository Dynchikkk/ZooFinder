using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Application.Features.Parks.Animals.Services.ParkAnimals;

public interface IParkAnimalService
{
    Task<ParkAnimalResponse> GetAsync(Guid parkAnimalId, CancellationToken cancellationToken);
    Task<OffsetPageResponse<ParkAnimalResponse>> SearchAsync(
        ParkAnimalSearchRequest request, CancellationToken cancellationToken);
    Task<ParkAnimalResponse> AddAsync(AddParkAnimalRequest request, CancellationToken cancellationToken);
    Task<ParkAnimalResponse> UpdateAsync(UpdateParkAnimalRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid parkAnimalId, CancellationToken cancellationToken);
}
