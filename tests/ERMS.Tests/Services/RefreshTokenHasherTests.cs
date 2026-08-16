using ERMS.Infrastructure.Authentication;

namespace ERMS.Tests.Services;

/// <summary>
/// AuthService'in DB'ye yazdığı değerin gerçekten "hash" olma özelliklerini taşıdığını
/// doğrular: deterministik (aynı girdi -> aynı çıktı, aksi halde RefreshAsync token'ı
/// asla bulamaz) ve girdiden farklı (aksi halde düz metin saklamaktan bir farkı kalmaz).
/// </summary>
public class RefreshTokenHasherTests
{
    private readonly RefreshTokenHasher _sut = new();

    [Fact]
    public void Hash_SameInput_ReturnsSameHash()
    {
        const string token = "ayni-token-degeri";

        var first = _sut.Hash(token);
        var second = _sut.Hash(token);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Hash_DifferentInputs_ReturnDifferentHashes()
    {
        var hashA = _sut.Hash("token-a");
        var hashB = _sut.Hash("token-b");

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void Hash_NeverReturnsThePlainInput()
    {
        const string token = "duz-metin-token";

        var hash = _sut.Hash(token);

        Assert.NotEqual(token, hash);
    }
}
