using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Domain.Users;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");
        builder.ConfigureBase();
        builder.Property(account => account.Login).HasMaxLength(100);
        builder.Property(account => account.PasswordHash).HasMaxLength(2048);
        builder.HasIndex(account => account.Login).IsUnique();
    }
}
