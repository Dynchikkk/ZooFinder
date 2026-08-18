using ZooFinder.Domain.BaseEntities;

namespace ZooFinder.Domain.Users;

public class UserProfile : IdEntity<Guid>
{
    public required string DisplayName { get; set; }

    // Navigation

    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
}
