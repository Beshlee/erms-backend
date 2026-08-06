using ERMS.Application.Abstractions.Authentication;

namespace ERMS.Infrastructure.Authentication;

/// <summary>BCrypt tabanlı parola hash'leme (FR-02).</summary>
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
