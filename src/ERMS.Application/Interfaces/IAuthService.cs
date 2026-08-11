using ERMS.Application.DTOs.Auth;

namespace ERMS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Bonus — refresh token akışı: geçerli bir refresh token'ı yeni bir JWT'ye çevirir (rotation).</summary>
    Task<LoginResponseDto> RefreshAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Bonus — refresh token'ı geçersiz kılar (çıkış). Bilinmeyen bir token için sessizce başarılı sayılır.</summary>
    Task RevokeRefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default);
}
