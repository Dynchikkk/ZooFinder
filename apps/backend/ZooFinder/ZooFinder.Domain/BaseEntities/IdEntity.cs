namespace ZooFinder.Domain.BaseEntities;

public abstract class IdEntity<TId>
{
    public required TId Id { get; set; }
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}
