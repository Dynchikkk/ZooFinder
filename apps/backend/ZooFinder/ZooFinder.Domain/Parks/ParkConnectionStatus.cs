namespace ZooFinder.Domain.Parks;

public enum ParkConnectionStatus
{
    None = 0,
    Submitted = 1,
    AwaitingPayment = 2,
    Activated = 3,
    Rejected = 4,
    Cancelled = 5
}
