namespace ERMS.Application.Common.Exceptions;

/// <summary>
/// Beklenen (öngörülebilir) iş hatalarının temel sınıfı — ör. "kullanıcı bulunamadı",
/// "bu talep sana ait değil". Servis katmanı bu türden (ya da alt sınıflarından, örn.
/// <see cref="NotFoundAppException"/>) bir istisna FIRLATIR; controller'lar bunu YAKALAMAZ,
/// çünkü ERMS.Api/Middleware/ExceptionHandlingMiddleware.cs bunu tek bir yerden yakalayıp
/// Bölüm 5.6'daki standart hata modeline ({ code, message }) çevirip HTTP yanıtı üretir.
/// Bu sayede her controller action'ında tekrar tekrar try/catch yazmaya gerek kalmaz.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }
    public string Code { get; }
}
