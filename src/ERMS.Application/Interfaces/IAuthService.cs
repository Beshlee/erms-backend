using ERMS.Application.DTOs.Auth;

namespace ERMS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);
}
