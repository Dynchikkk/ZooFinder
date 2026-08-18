using ZooFinder.Domain.Animals;
using ZooFinder.Domain.BaseEntities;

namespace ZooFinder.Domain.Discussions;

public class DiscussionRoom : IdEntity<Guid>
{
    public DiscussionRoomType Type { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public bool IsClosed { get; set; }

    // Navigation

    public Guid AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;

    public ICollection<DiscussionMessage> DiscussionMessages { get; set; } = [];
}
