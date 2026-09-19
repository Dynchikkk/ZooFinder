using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZooFinder.Domain.BaseEntities;

namespace ZooFinder.Infrastructure.Persistence.Configurations;

internal static class EntityConfiguration
{
    public static void ConfigureBase<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : IdEntity<Guid>
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
