namespace ERMS.Application.Common.Exceptions;

/// <summary>
/// Application katmanı ASP.NET Core'a (Microsoft.AspNetCore.Http.StatusCodes) bağımlı
/// olmasın diye kullanılan sabit HTTP durum kodları (Bölüm 5.6 — Standart Hata Modeli).
/// </summary>
internal static class AppStatusCodes
{
    public const int Status400BadRequest = 400;
    public const int Status401Unauthorized = 401;
    public const int Status403Forbidden = 403;
    public const int Status404NotFound = 404;
    public const int Status409Conflict = 409;
}
