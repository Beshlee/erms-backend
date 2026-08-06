using System.Security.Claims;
using ERMS.Application.Abstractions.Authentication;
using ERMS.Domain.Enums;

namespace ERMS.Api.Authentication;

/// <summary>
/// ICurrentUserService'in HttpContext'e bağımlı implementasyonu — bu yüzden
/// Application değil, Api katmanında yaşar (bkz. ICurrentUserService.cs yorumu).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;

            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.Role)?.Value;

            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }
}
