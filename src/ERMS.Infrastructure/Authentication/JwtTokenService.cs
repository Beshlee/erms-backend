using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ERMS.Application.Abstractions.Authentication;
using ERMS.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERMS.Infrastructure.Authentication;

/// <summary>JWT üretimi (FR-03). Claim'ler: sub, email, role, name.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public JwtResult GenerateToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("name", $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);

        return new JwtResult(tokenString, expiresAtUtc, refreshToken, refreshTokenExpiresAtUtc);
    }

    private static string GenerateRefreshToken()
    {
        // JWT'nin aksine kendi içinde bilgi taşımaz — DB'de saklanan, kriptografik olarak
        // tahmin edilemez rastgele bir anahtar/referanstır (bkz. AuthService.RefreshAsync).
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
