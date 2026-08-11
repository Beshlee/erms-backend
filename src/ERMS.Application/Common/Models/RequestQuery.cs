using ERMS.Domain.Enums;

namespace ERMS.Application.Common.Models;

/// <summary>
/// Talep listeleme filtreleri (FR-27, FR-28, FR-29). Priority/CreatedFrom..MaxAmount,
/// Bölüm 8.3 bonus — "Global arama ve gelişmiş filtreleme (birden çok kritere göre)".
/// </summary>
public sealed class RequestQuery
{
    public RequestStatus? Status { get; init; }
    public int? RequestTypeId { get; init; }

    /// <summary>Başlık, açıklama ve talep türü adında arar (global arama).</summary>
    public string? Search { get; init; }

    public RequestPriority? Priority { get; init; }

    /// <summary>Oluşturulma tarihi bu tarihten itibaren (dahil).</summary>
    public DateTime? CreatedFrom { get; init; }

    /// <summary>Oluşturulma tarihi bu tarihe kadar (dahil, günün sonuna kadar).</summary>
    public DateTime? CreatedTo { get; init; }

    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
