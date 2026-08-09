namespace ERMS.Application.DTOs.Requests;

/// <summary>FR-41/42 — bir durum değişikliği kaydı (audit log satırı).</summary>
public sealed record RequestHistoryItemDto(
    string OldStatus,
    string NewStatus,
    string? Note,
    DateTime ChangedAt,
    string ChangedByName);
