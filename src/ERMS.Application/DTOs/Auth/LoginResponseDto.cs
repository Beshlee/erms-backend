namespace ERMS.Application.DTOs.Auth;

/// <summary>
/// Bölüm 5.2 — POST /api/auth/login 200 OK sözleşmesi. RefreshToken — bonus (refresh token akışı).
/// </summary>
public sealed record LoginResponseDto(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    UserSummaryDto User);

public sealed record UserSummaryDto(
    int Id,
    string FullName,
    string Role);
