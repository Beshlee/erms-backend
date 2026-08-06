using ERMS.Domain.Enums;

namespace ERMS.Application.DTOs.Requests;

/// <summary>
/// PUT /api/requests/{id} istek gövdesi — yalnızca Draft durumundaki talepler için (FR-22).
/// Durumu değiştirmez; Draft → Pending geçişi ayrı bir uç noktadır (POST .../submit, Gün 6).
/// </summary>
public sealed record UpdateRequestDto(
    int RequestTypeId,
    string Title,
    string Description,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal? Amount,
    RequestPriority Priority);
