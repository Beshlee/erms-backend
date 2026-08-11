using ERMS.Domain.Entities;

namespace ERMS.Application.Abstractions.Authentication;

/// <summary>
/// JWT access token üretimi (FR-03). RefreshToken de burada üretilir (bonus — refresh token
/// akışı) çünkü ikisi de "token" kriptografisi/ayarlarıyla ilgili — Application katmanı
/// yalnızca sonucu alıp DB'ye kalıcı hale getirmekle (AuthService) ilgilenir.
/// </summary>
public interface IJwtTokenService
{
    JwtResult GenerateToken(User user);
}

public sealed record JwtResult(
    string Token,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
