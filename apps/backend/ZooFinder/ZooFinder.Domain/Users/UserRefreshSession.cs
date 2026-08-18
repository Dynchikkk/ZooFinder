using ZooFinder.Domain.BaseEntities;

namespace ZooFinder.Domain.Users;

public class UserRefreshSession : IdEntity<Guid>
{
    public required string RefreshTokenHash { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    // Navigation

    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
}
