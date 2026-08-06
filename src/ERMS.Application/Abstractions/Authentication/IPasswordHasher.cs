namespace ERMS.Application.Abstractions.Authentication;

/// <summary>Parola hash'leme (FR-02 — düz metin asla saklanmaz).</summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);

    bool Verify(string plainPassword, string passwordHash);
}
