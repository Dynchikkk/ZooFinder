using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Application.Features.Discussions.Messages.Constants;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class DiscussionMessageConfiguration : IEntityTypeConfiguration<DiscussionMessage>
{
    public void Configure(EntityTypeBuilder<DiscussionMessage> builder)
    {
        builder.ToTable("DiscussionMessages");
        builder.ConfigureBase();
        builder.Property(message => message.Content).HasMaxLength(DiscussionMessageConstraints.MaximumContentLength);
        builder.HasIndex(message => new { message.DiscussionRoomId, message.CreatedAtUtc, message.Id });
        builder.HasOne(message => message.DiscussionRoom).WithMany(room => room.DiscussionMessages)
            .HasForeignKey(message => message.DiscussionRoomId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasOne(message => message.AuthorUserAccount).WithMany(account => account.AuthoredDiscussionMessages)
            .HasForeignKey(message => message.AuthorUserAccountId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasQueryFilter(message => !message.IsDeleted &&
            !message.DiscussionRoom.IsDeleted && !message.DiscussionRoom.Animal.IsDeleted);
    }
}
