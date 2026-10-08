using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Application.Features.Parks.Catalog.Constants;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class ParkConfiguration : IEntityTypeConfiguration<Park>
{
    public void Configure(EntityTypeBuilder<Park> builder)
    {
        builder.ToTable("Parks");
        builder.ConfigureBase();
        builder.Property(park => park.Name).HasMaxLength(ParkConstraints.MaximumNameLength);
        builder.Property(park => park.Slug).HasMaxLength(ParkConstraints.MaximumSlugLength);
        builder.Property(park => park.Description).HasMaxLength(ParkConstraints.MaximumDescriptionLength);
        builder.Property(park => park.Address).HasMaxLength(ParkConstraints.MaximumAddressLength);
        builder.HasIndex(park => park.Slug).IsUnique();
        builder.HasOne(park => park.OwnerUserAccount).WithMany()
            .HasForeignKey(park => park.OwnerUserAccountId).OnDelete(DeleteBehavior.ClientNoAction);
    }
}
