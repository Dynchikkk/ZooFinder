using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Application.Features.Parks.Animals.Constants;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class ParkAnimalConfiguration : IEntityTypeConfiguration<ParkAnimal>
{
    public void Configure(EntityTypeBuilder<ParkAnimal> builder)
    {
        builder.ToTable("ParkAnimals");
        builder.ConfigureBase();
        builder.Property(parkAnimal => parkAnimal.LocalDescription)
            .HasMaxLength(ParkAnimalConstraints.MaximumLocalDescriptionLength);
        builder.HasIndex(parkAnimal => new { parkAnimal.ParkId, parkAnimal.AnimalId }).IsUnique();
        builder.HasOne(parkAnimal => parkAnimal.Park).WithMany(park => park.ParkAnimals)
            .HasForeignKey(parkAnimal => parkAnimal.ParkId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasOne(parkAnimal => parkAnimal.Animal).WithMany(animal => animal.ParkAnimals)
            .HasForeignKey(parkAnimal => parkAnimal.AnimalId).OnDelete(DeleteBehavior.ClientNoAction);
        builder.HasQueryFilter(parkAnimal => !parkAnimal.IsDeleted && !parkAnimal.Park.IsDeleted && !parkAnimal.Animal.IsDeleted);
    }
}
