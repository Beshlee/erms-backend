namespace ERMS.Application.DTOs.Approvals;

/// <summary>POST /api/approvals/{requestId}/approve — not opsiyoneldir (Bölüm 5.5).</summary>
public sealed record ApproveRequestDto(string? Comment);
