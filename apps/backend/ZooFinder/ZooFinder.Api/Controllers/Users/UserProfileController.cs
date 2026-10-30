using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Api.Authentication;
using ZooFinder.Application.Modules.Users.Modules.Profiles.Services.UserProfiles;

namespace ZooFinder.Api.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> Get(CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        UserProfileResponse response = await _userProfileService.GetProfileAsync(userAccountId, cancellationToken);
        return Ok(response);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> Update(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        UserProfileResponse response = await _userProfileService.UpdateProfileAsync(userAccountId, request, cancellationToken);
        return Ok(response);
    }
}
