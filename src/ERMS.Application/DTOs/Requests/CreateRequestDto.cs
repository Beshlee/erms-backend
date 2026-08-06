using ERMS.Domain.Enums;

namespace ERMS.Application.DTOs.Requests;

/// <summary>Bölüm 5.3 — POST /api/requests istek gövdesi.</summary>
public sealed record CreateRequestDto(
    int RequestTypeId,
    string Title,
    string Description,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal? Amount,
    RequestPriority Priority,
    bool SaveAsDraft);
