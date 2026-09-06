namespace ZooFinder.Application.Features.Users.Contracts;

public sealed record UserProfileResponse(
    Guid UserAccountId,
    string DisplayName);
