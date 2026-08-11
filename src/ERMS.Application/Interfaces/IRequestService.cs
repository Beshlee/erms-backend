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

    /// <summary>
    /// FR-25..29 — giriş yapan kullanıcının kendi taleplerini filtreli/sayfalı listeler.
    /// priority/createdFrom..maxAmount — Bölüm 8.3 bonus (global arama + gelişmiş filtreleme).
    /// </summary>
    Task<PagedResult<RequestResponseDto>> GetMyRequestsAsync(
        string? status,
        int? requestTypeId,
        string? search,
        string? priority,
        DateTime? createdFrom,
        DateTime? createdTo,
        decimal? minAmount,
        decimal? maxAmount,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>FR-23/24 — taslağı gönderir (Draft → Pending/Approved).</summary>
    Task<RequestResponseDto> SubmitAsync(
        int requestId,
        CancellationToken cancellationToken = default);

    /// <summary>FR-30 — bekleyen (Pending) bir talebi iptal eder.</summary>
    Task<RequestResponseDto> CancelAsync(
        int requestId,
        CancellationToken cancellationToken = default);

    /// <summary>FR-42 — talep detayı ve kronolojik durum geçmişi.</summary>
    Task<RequestDetailDto> GetDetailAsync(
        int requestId,
        CancellationToken cancellationToken = default);
}
