using ERMS.Application.DTOs.Admin;

namespace ERMS.Application.Interfaces;

/// <summary>FR-13, FR-14, FR-15 — admin talep türü yönetimi.</summary>
public interface IRequestTypeService
{
    Task<RequestTypeResponseDto> CreateAsync(
        CreateRequestTypeDto dto,
        CancellationToken cancellationToken = default);

    Task<RequestTypeResponseDto> UpdateAsync(
        int requestTypeId,
        UpdateRequestTypeDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Admin görünümü — pasif olanlar dahil hepsi.</summary>
    Task<List<RequestTypeResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>GET /api/request-types (Herkes/auth) — FR-15: yalnızca aktif türler.</summary>
    Task<List<RequestTypeResponseDto>> GetActiveAsync(
        CancellationToken cancellationToken = default);
}
