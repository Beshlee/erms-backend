namespace ERMS.Application.DTOs.Approvals;

/// <summary>Bölüm 5.5 — onay/red işlemi sonrası yanıt gövdesi.</summary>
public sealed record ApprovalResultDto(
    int RequestId,
    string Status,
    string DecidedBy,
    DateTime DecidedAt);
