using ZooFinder.Domain.BaseEntities;

namespace ZooFinder.Domain.Parks;

public class ParkConnectionRequest : IdEntity<Guid>
{
    public string? ContactName { get; set; }
    public string? Contact { get; set; }
    public ParkConnectionStatus Status { get; set; } = ParkConnectionStatus.Submitted;
    public decimal? AgreedAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? Comment { get; set; }

    // Navigation

    public Guid ParkId { get; set; }
    public Park Park { get; set; } = null!;
}
