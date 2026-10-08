using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ZooFinder.Application.Common.Animals.Information.Constants;
using ZooFinder.Domain.Animals;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("Animals");
        builder.ConfigureBase();
        builder.Property(animal => animal.InformationSource)
            .HasMaxLength(AnimalInformationConstraints.MaximumInformationSourceLength);
        builder.Property(animal => animal.SourceItemId).HasMaxLength(AnimalInformationConstraints.MaximumSourceItemIdLength);
        builder.Property(animal => animal.LanguageCode).HasMaxLength(2);
        builder.HasIndex(animal => new { animal.InformationSource, animal.LanguageCode, animal.SourceItemId }).IsUnique();
        builder.HasIndex(animal => new { animal.LanguageCode, animal.CreatedAtUtc, animal.Id });
    }
}
