using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class DiscussionRoomConfiguration : IEntityTypeConfiguration<DiscussionRoom>
{
    public void Configure(EntityTypeBuilder<DiscussionRoom> builder)
    {
        builder.ToTable("DiscussionRooms");
        builder.ConfigureBase();
        builder.Property(room => room.Name).HasMaxLength(100);
        builder.HasIndex(room => new { room.AnimalId, room.Name }).IsUnique();
        builder.HasOne(room => room.Animal).WithMany(animal => animal.DiscussionRooms)
            .HasForeignKey(room => room.AnimalId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasQueryFilter(room => !room.IsDeleted && !room.Animal.IsDeleted);
    }
}
