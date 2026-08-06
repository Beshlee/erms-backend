namespace ERMS.Application.DTOs.Auth;

/// <summary>Bölüm 5.2 — POST /api/auth/login 200 OK sözleşmesi.</summary>
public sealed record LoginResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserSummaryDto User);

public sealed record UserSummaryDto(
    int Id,
    string FullName,
    string Role);
