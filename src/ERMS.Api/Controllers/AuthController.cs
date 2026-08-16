using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.DTOs.Auth;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    /// <summary>
    /// FR-01, FR-02, FR-03 — e-posta/parola ile giriş, JWT üretir.
    /// Review bulgusu: IP başına dakikada 5 denemeyle sınırlı (bkz. Program.cs → "auth" politikası)
    /// — otomatik parola deneme saldırılarını yavaşlatmak için.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
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

    /// <summary>
    /// Bonus — refresh token akışı: geçerli bir refresh token'ı yeni bir JWT'ye çevirir
    /// (rotation). Access token süresi dolmuş olabileceği için [AllowAnonymous] — burada
    /// güvenliği sağlayan Authorization header'ı değil, gövdedeki refresh token'dır.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Refresh(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>Bonus — refresh token akışı: refresh token'ı geçersiz kılar (sunucu tarafında çıkış).</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request, cancellationToken);

        return NoContent();
    }
}
