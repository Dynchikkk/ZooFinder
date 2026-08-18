using ZooFinder.Domain.BaseEntities;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Domain.Users;

public class UserAccount : IdEntity<Guid>
{
    public string? Login { get; set; }
    public string? PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Navigation

    public UserProfile UserProfile { get; set; } = null!;

    public ICollection<DiscussionMessage> AuthoredDiscussionMessages { get; set; } = [];
    public ICollection<UserRefreshSession> UserRefreshSessions { get; set; } = [];
}
