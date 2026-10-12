namespace ZooFinder.Application.Modules.Users.Modules.Profiles.Services.UserProfiles;

public sealed record UpdateUserProfileRequest(string DisplayName);

public sealed record UserProfileResponse(
    Guid UserAccountId,
    string DisplayName);
