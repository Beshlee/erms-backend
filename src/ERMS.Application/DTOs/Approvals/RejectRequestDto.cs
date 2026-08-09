namespace ERMS.Application.DTOs.Approvals;

/// <summary>POST /api/approvals/{requestId}/reject — gerekçe (Comment) zorunludur (FR-34).</summary>
public sealed record RejectRequestDto(string Comment);
