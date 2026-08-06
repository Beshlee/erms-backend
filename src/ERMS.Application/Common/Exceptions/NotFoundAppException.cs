namespace ERMS.Application.Common.Exceptions;

/// <summary>404 — kayıt bulunamadı (FR-45).</summary>
public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message)
        : base(AppStatusCodes.Status404NotFound, "NOT_FOUND", message)
    {
    }
}
