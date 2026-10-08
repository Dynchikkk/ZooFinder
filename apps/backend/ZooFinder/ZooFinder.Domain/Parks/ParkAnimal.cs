using ZooFinder.Domain.Animals;
using ZooFinder.Domain.BaseEntities;

namespace ZooFinder.Domain.Parks;

public class ParkAnimal : IdEntity<Guid>
{
    public string? LocalDescription { get; set; }
    public bool IsPublished { get; set; } = true;

    // Navigation

    public Guid ParkId { get; set; }
    public Park Park { get; set; } = null!;

    public Guid AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
}
