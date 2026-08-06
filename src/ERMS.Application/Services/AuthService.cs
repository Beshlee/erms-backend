using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Auth;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;

namespace ERMS.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IRepository<User> userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
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

        var jwt = _jwtTokenService.GenerateToken(user);

        return new LoginResponseDto(
            jwt.Token,
            jwt.ExpiresAtUtc,
            new UserSummaryDto(
                user.Id,
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString()));
    }
}
