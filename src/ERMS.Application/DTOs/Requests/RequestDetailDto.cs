namespace ERMS.Application.DTOs.Requests;

/// <summary>GET /api/requests/{id} — talep detayı, kronolojik durum geçmişi (FR-42) ve yorumlar (FR-39).</summary>
public sealed record RequestDetailDto(
    int Id,
    string Title,
    string Description,
    string Type,
    string Status,
    string Priority,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal? Amount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int RequesterId,
    string RequesterName,
    IReadOnlyList<RequestHistoryItemDto> History,
    IReadOnlyList<CommentResponseDto> Comments);
