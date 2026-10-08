using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Application.Features.Parks.Connections.Constants;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class ParkConnectionConfiguration : IEntityTypeConfiguration<ParkConnectionRequest>
{
    public void Configure(EntityTypeBuilder<ParkConnectionRequest> builder)
    {
        builder.ToTable("ParkConnectionRequests");
        builder.ConfigureBase();
        builder.Property(request => request.ContactName).HasMaxLength(ParkConnectionConstraints.MaximumContactNameLength);
        builder.Property(request => request.Contact).HasMaxLength(ParkConnectionConstraints.MaximumContactLength);
        builder.Property(request => request.Comment).HasMaxLength(ParkConnectionConstraints.MaximumCommentLength);
        builder.Property(request => request.Currency).HasMaxLength(3);
        builder.Property(request => request.AgreedAmount).HasPrecision(17, 2);
        builder.HasIndex(request => new { request.ParkId, request.Status });
        builder.HasIndex(request => request.ParkId).IsUnique().HasFilter("[IsDeleted] = 0 AND [Status] IN (1, 2)");
        builder.HasOne(request => request.Park).WithMany(park => park.ConnectionRequests)
            .HasForeignKey(request => request.ParkId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasQueryFilter(request => !request.IsDeleted && !request.Park.IsDeleted);
    }
}
