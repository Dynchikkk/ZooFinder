using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Application.Features.Users.Constants;
using ZooFinder.Domain.Users;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.ConfigureBase();
        builder.Property(profile => profile.DisplayName).HasMaxLength(UserProfileConstraints.MaximumDisplayNameLength);
        builder.HasOne(profile => profile.UserAccount).WithOne(account => account.UserProfile)
            .HasForeignKey<UserProfile>(profile => profile.UserAccountId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasIndex(profile => profile.UserAccountId).IsUnique();
        builder.HasQueryFilter(profile => !profile.IsDeleted && !profile.UserAccount.IsDeleted);
    }
}
