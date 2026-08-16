namespace ERMS.Application.Abstractions.Authentication;

/// <summary>
/// Refresh token'ların veritabanında düz metin DEĞİL, bu arayüzden geçirilmiş hâliyle
/// saklanması için (bkz. AuthService.IssueTokensAsync/RefreshAsync/RevokeRefreshTokenAsync).
///
/// IPasswordHasher (BCrypt) neden kullanılmıyor: BCrypt kasıtlı olarak YAVAŞ ve her
/// çağrıda FARKLI bir sonuç üretir (salt) — bu, düşük entropili parolaları kaba kuvvetle
/// denemeyi zorlaştırmak içindir, ama bu yüzden "bana bu token'a sahip kullanıcıyı bul"
/// gibi DB'de tek bir WHERE ile arama yapılmasını da imkansız kılar (BCrypt hash'i indekslenip
/// eşitlik karşılaştırmasıyla aranamaz). Refresh token'lar ise zaten 64 rastgele byte'lık
/// (512 bit) kriptografik olarak tahmin edilemez değerlerdir (bkz. JwtTokenService.
/// GenerateRefreshToken) — düşük entropili bir parola gibi kaba kuvvetle denenebilir
/// değildir, bu yüzden hızlı/deterministik bir hash (SHA-256) hem yeterince güvenli hem de
/// DB'de aranabilir olması için gereklidir.
/// </summary>
public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}
