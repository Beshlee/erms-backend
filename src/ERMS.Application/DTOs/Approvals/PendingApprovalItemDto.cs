namespace ERMS.Application.DTOs.Approvals;

/// <summary>GET /api/approvals/pending — onay bekleyen bir talep satırı (Ekran 5).</summary>
public sealed record PendingApprovalItemDto(
    int Id,
    string Title,
    string RequesterName,
    string Type,
    string Status,
    DateTime CreatedAt);
