using ERMS.Domain.Entities;

namespace ERMS.Application.Abstractions.Authentication;

/// <summary>JWT access token üretimi (FR-03).</summary>
public interface IJwtTokenService
{
    JwtResult GenerateToken(User user);
}

public sealed record JwtResult(string Token, DateTime ExpiresAtUtc);
