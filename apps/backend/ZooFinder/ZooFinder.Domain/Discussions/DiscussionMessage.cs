using ZooFinder.Domain.BaseEntities;
using ZooFinder.Domain.Users;

namespace ZooFinder.Domain.Discussions;

public class DiscussionMessage : IdEntity<Guid>
{
    public required string Content { get; set; }

    public DateTime? EditedAtUtc { get; set; }

    // Navigation

    public Guid DiscussionRoomId { get; set; }
    public DiscussionRoom DiscussionRoom { get; set; } = null!;

    public Guid AuthorUserAccountId { get; set; }
    public UserAccount AuthorUserAccount { get; set; } = null!;
}
