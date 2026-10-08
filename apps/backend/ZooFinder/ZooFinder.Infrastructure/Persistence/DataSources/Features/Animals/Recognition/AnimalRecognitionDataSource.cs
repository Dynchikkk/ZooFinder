using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Features.Animals.Recognition.DataSources;

namespace ZooFinder.Infrastructure.Persistence.DataSources.Features.Animals.Recognition;

public sealed class AnimalRecognitionDataSource(ZooFinderDbContext dbContext) : IAnimalRecognitionDataSource
{
    public async Task<ParkRecognitionContextResult?> GetParkContextAsync(Guid parkId, CancellationToken cancellationToken)
    {
        var park = await dbContext.Parks.AsNoTracking().SingleOrDefaultAsync(park => park.Id == parkId, cancellationToken);
        if (park == null) return null;
        var animals = await dbContext.ParkAnimals.AsNoTracking().Where(link => link.ParkId == parkId && link.IsPublished)
            .OrderBy(link => link.Animal.Title).ThenBy(link => link.Id)
            .Select(link => new ParkRecognitionAnimalResult(
                link.Id, link.AnimalId, link.Animal.Title, link.Animal.ScientificName))
            .ToListAsync(cancellationToken);
        return new ParkRecognitionContextResult(park.Id, park.Status, animals);
    }
}
