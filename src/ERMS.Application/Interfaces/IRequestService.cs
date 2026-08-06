using ERMS.Application.Common.Models;
using ERMS.Application.DTOs.Requests;

namespace ERMS.Application.Interfaces;

public interface IRequestService
{
    Task<RequestResponseDto> CreateAsync(
        CreateRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<RequestResponseDto> UpdateAsync(
        int requestId,
        UpdateRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>FR-25..29 — giriş yapan kullanıcının kendi taleplerini filtreli/sayfalı listeler.</summary>
    Task<PagedResult<RequestResponseDto>> GetMyRequestsAsync(
        string? status,
        int? requestTypeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
