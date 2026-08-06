using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers.Admin;

/// <summary>
/// GEÇİCİ: Yalnızca rol tabanlı yetkilendirmeyi (FR-06 — 403 Forbidden) uçtan uca
/// doğrulamak için eklenen minimal bir uç nokta. Asıl admin CRUD endpoint'leri
/// (kullanıcı/departman/talep türü yönetimi) Gün 8'de burada dolacak.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong (Admin rolüyle erişildi)" });
    }
}
