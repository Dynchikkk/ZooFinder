using ZooFinder.Domain.Parks;

namespace ZooFinder.Api.Controllers.Parks;

public sealed record UpdateParkHttpRequest(
    string Name,
    string Slug,
    string? Description = null,
    string? Address = null);

public sealed record SetParkStatusRequest(ParkStatus Status);

public sealed record UpdateParkAnimalHttpRequest(
    string? LocalDescription,
    bool IsPublished);
