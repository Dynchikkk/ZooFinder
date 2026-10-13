using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.DataSources;
using ZooFinder.Infrastructure.Persistence;

namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.DataSources;

public sealed class AnimalRecognitionDataSource : IAnimalRecognitionDataSource
{
    private readonly ZooFinderDbContext _dbContext;

    public AnimalRecognitionDataSource(ZooFinderDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<ParkRecognitionContextResult?> GetParkContextAsync(Guid parkId, CancellationToken cancellationToken)
    {
        var park = await _dbContext.Parks
            .AsNoTracking()
            .SingleOrDefaultAsync(park => park.Id == parkId, cancellationToken);

        if (park == null)
        {
            return null;
        }

        var animals = await _dbContext.ParkAnimals
            .AsNoTracking()
            .Where(link => link.ParkId == parkId && link.IsPublished)
            .OrderBy(link => link.Animal.Title)
            .ThenBy(link => link.Id)
            .Select(link => new ParkRecognitionAnimalResult(
                link.Id, link.AnimalId, link.Animal.Title, link.Animal.ScientificName))
            .ToListAsync(cancellationToken);

        return new ParkRecognitionContextResult(park.Id, park.Status, animals);
    }
}
