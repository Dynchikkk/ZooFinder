namespace ZooFinder.Application.Features.Users.Services.UserProfiles;

public sealed record UpdateUserProfileRequest(string DisplayName);

public sealed record UserProfileResponse(
    Guid UserAccountId,
    string DisplayName);
