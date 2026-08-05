using ERMS.Domain.Enums;

namespace ERMS.Application.Common.Models;

/// <summary>Talep listeleme filtreleri (FR-27, FR-28, FR-29).</summary>
public sealed class RequestQuery
{
    public RequestStatus? Status { get; init; }
    public int? RequestTypeId { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
