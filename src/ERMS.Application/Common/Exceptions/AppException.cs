namespace ERMS.Application.Common.Exceptions;

/// <summary>
/// Beklenen (öngörülebilir) iş hatalarının temel sınıfı. Global exception middleware
/// bunu yakalayıp Bölüm 5.6'daki standart hata modeline ({ code, message }) çevirir.
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
