using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Domain.Users;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class UserRefreshSessionConfiguration : IEntityTypeConfiguration<UserRefreshSession>
{
    public void Configure(EntityTypeBuilder<UserRefreshSession> builder)
    {
        builder.ToTable("UserRefreshSessions");
        builder.ConfigureBase();
        builder.Property(session => session.RefreshTokenHash).HasMaxLength(512);
        builder.HasIndex(session => session.RefreshTokenHash).IsUnique();
        builder.HasIndex(session => session.UserAccountId);
        builder.HasOne(session => session.UserAccount).WithMany(account => account.UserRefreshSessions)
            .HasForeignKey(session => session.UserAccountId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasQueryFilter(session => !session.IsDeleted && !session.UserAccount.IsDeleted);
    }
}
