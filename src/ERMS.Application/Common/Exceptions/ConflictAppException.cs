namespace ERMS.Application.Common.Exceptions;

/// <summary>409 — geçersiz durum geçişi, ör. onaylanmış talebi düzenleme (Bölüm 5.6).</summary>
public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message)
        : base(AppStatusCodes.Status409Conflict, "CONFLICT", message)
    {
    }
}
