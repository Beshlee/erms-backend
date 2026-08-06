using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.DTOs.Auth;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    /// <summary>FR-01, FR-02, FR-03 — e-posta/parola ile giriş, JWT üretir.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Token'ı olan herhangi bir rol erişebilir — FR-04'ü (tokensiz istekte 401) doğrulamak
    /// için kullanılabilecek basit bir uç nokta.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = _currentUserService.UserId,
            role = _currentUserService.Role?.ToString()
        });
    }
}
