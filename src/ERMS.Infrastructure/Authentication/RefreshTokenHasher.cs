using System.Security.Cryptography;
using System.Text;
using ERMS.Application.Abstractions.Authentication;

namespace ERMS.Infrastructure.Authentication;

/// <summary>
/// SHA-256 tabanlı, deterministik refresh token hash'leme — bkz. IRefreshTokenHasher'daki
/// "neden BCrypt değil" gerekçesi. Aynı token her zaman aynı hash'i üretir, böylece
/// AuthService.RefreshAsync/RevokeRefreshTokenAsync bir WHERE RefreshToken = @hash sorgusuyla
/// kullanıcıyı bulabilir.
/// </summary>
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
