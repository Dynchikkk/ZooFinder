using ZooFinder.Domain.BaseEntities;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Domain.Animals;

public class Animal : IdEntity<Guid>
{
    public long WikipediaPageId { get; set; }
    public required string WikipediaLanguageCode { get; set; }
    public required string Title { get; set; }

    public string? ScientificName { get; set; }
    public string? ShortDescription { get; set; }
    public string? ImageUrl { get; set; }

    public DateTime LastSynchronizedAtUtc { get; set; }

    // Navigation

    public ICollection<DiscussionRoom> DiscussionRooms { get; set; } = [];
}
