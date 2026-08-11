using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Auth;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;

namespace ERMS.Application.Services;

/// <summary>
/// Giriş (FR-01..03) ve bonus refresh token akışı. Şifre karşılaştırması IPasswordHasher'a,
/// JWT/refresh token üretimi IJwtTokenService'e devredilir — bu sınıf yalnızca "kimin,
/// hangi koşullarda giriş yapabileceği/token yenileyebileceği" iş kuralını uygular.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IRepository<User> userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            u => u.Email == request.Email,
            cancellationToken);

        // Yanlış parola ile pasif kullanıcı için AYNI hata mesajı/kodu döner —
        // aksi halde "bu e-posta var ama hesap pasif" bilgisi dışarı sızar (US-01).
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException(
                "E-posta veya parola hatalı.",
                "INVALID_CREDENTIALS");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<LoginResponseDto> RefreshAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            u => u.RefreshToken == request.RefreshToken,
            cancellationToken);

        // Bulunamadı, süresi dolmuş ya da kullanıcı bu arada pasifleştirilmiş — hepsi aynı
        // genel hatayla reddedilir (LoginAsync'teki bilgi-sızdırmama yaklaşımıyla tutarlı).
        if (user is null
            || !user.IsActive
            || user.RefreshTokenExpiresAt is null
            || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAppException(
                "Oturum süresi dolmuş, lütfen tekrar giriş yapın.",
                "INVALID_REFRESH_TOKEN");
        }

        // Rotation: her yenilemede hem access hem refresh token yenilenir, öncekiler
        // geçersiz olur — çalınmış bir refresh token'ın sınırsız kullanılmasını engeller.
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            u => u.RefreshToken == request.RefreshToken,
            cancellationToken);

        // Bilinmeyen/zaten geçersiz bir token için sessizce başarılı sayılır — çıkış
        // işleminin başarısız görünmesi için bir sebep yok, ortada zaten geçersiz token var.
        if (user is null)
        {
            return;
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<LoginResponseDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var jwt = _jwtTokenService.GenerateToken(user);

        user.RefreshToken = jwt.RefreshToken;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiresAtUtc;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto(
            jwt.Token,
            jwt.ExpiresAtUtc,
            jwt.RefreshToken,
            new UserSummaryDto(
                user.Id,
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString()));
    }
}
