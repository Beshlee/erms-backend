namespace ERMS.Application.Common.Exceptions;

/// <summary>401 — kimlik doğrulama başarısız (FR-04, US-01).</summary>
public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message, string code = "UNAUTHORIZED")
        : base(AppStatusCodes.Status401Unauthorized, code, message)
    {
    }
}
