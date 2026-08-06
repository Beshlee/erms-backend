namespace ERMS.Application.Common.Exceptions;

/// <summary>403 — kimlik doğrulandı ama yetki yok (FR-06).</summary>
public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message)
        : base(AppStatusCodes.Status403Forbidden, "FORBIDDEN", message)
    {
    }
}
