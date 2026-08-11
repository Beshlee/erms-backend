using ERMS.Application.Abstractions.Authentication;

namespace ERMS.Infrastructure.Authentication;

/// <summary>
/// BCrypt tabanlı parola hash'leme (FR-02) — tek yönlüdür (hash'ten parolaya geri dönülemez)
/// ve kendi içinde rastgele bir "salt" barındırır, bu yüzden aynı parola her Hash()
/// çağrısında FARKLI bir sonuç üretir; buna rağmen Verify() doğru şekilde eşleştirebilir
/// (salt, üretilen hash string'inin içine gömülüdür). Bu yüzden veritabanında yalnızca
/// PasswordHash saklanır, düz metin parola hiçbir zaman kaydedilmez.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public bool Verify(string plainPassword, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
    }
}
