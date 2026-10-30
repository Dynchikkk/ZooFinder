using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZooFinder.Api.Authentication;
using ZooFinder.Application.Modules.Auth.Modules.Authentication.Services.Auth;

namespace ZooFinder.Api.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.RefreshSessionAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RevokeSessionRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeSessionAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        Guid userAccountId = User.GetUserAccountId();
        await _authService.RevokeAllSessionsAsync(userAccountId, cancellationToken);
        return NoContent();
    }
}
