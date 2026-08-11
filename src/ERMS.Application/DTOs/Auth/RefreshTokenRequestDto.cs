namespace ERMS.Application.DTOs.Auth;

/// <summary>Bonus — refresh token akışı. POST /api/auth/refresh ve POST /api/auth/logout gövdesi.</summary>
public sealed record RefreshTokenRequestDto(string RefreshToken);
