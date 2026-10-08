using ZooFinder.Domain.BaseEntities;
using ZooFinder.Domain.Users;

namespace ZooFinder.Domain.Parks;

public class Park : IdEntity<Guid>
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public ParkStatus Status { get; set; } = ParkStatus.Active;

    // Navigation

    public Guid? OwnerUserAccountId { get; set; }
    public UserAccount? OwnerUserAccount { get; set; }

    public ICollection<ParkAnimal> ParkAnimals { get; set; } = [];
    public ICollection<ParkConnectionRequest> ConnectionRequests { get; set; } = [];
}
