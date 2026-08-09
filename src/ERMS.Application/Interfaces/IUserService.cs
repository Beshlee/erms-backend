using ERMS.Application.DTOs.Admin;

namespace ERMS.Application.Interfaces;

/// <summary>FR-07..10, FR-12 — admin kullanıcı yönetimi.</summary>
public interface IUserService
{
    Task<UserResponseDto> CreateAsync(
        CreateUserDto dto,
        CancellationToken cancellationToken = default);

    Task<UserResponseDto> UpdateAsync(
        int userId,
        UpdateUserDto dto,
        CancellationToken cancellationToken = default);

    Task<List<UserResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
