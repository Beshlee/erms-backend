using ERMS.Application.Common.Models;
using ERMS.Domain.Entities;

namespace ERMS.Application.Abstractions.Persistence;

/// <summary>
/// Generic repository'nin yetersiz kaldığı, birden çok tabloyu (Include) ve
/// özel filtreleme/sayfalama mantığı gerektiren Request sorguları için.
/// Bu sayede Application katmanı IQueryable/Include gibi EF Core detaylarına
/// doğrudan bağımlı kalmaz; sorgu mantığı Infrastructure'da izole edilir.
/// </summary>
public interface IRequestQueryRepository
{
    /// <summary>Bir çalışanın kendi taleplerini filtreli/sayfalı getirir (FR-25..29).</summary>
    Task<PagedResult<Request>> GetEmployeeRequestsAsync(
        int employeeId,
        RequestQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Bir yöneticiye bağlı personelin bekleyen taleplerini getirir (FR-32).</summary>
    Task<List<Request>> GetPendingManagerRequestsAsync(
        int managerId,
        CancellationToken cancellationToken = default);

    /// <summary>Talep türü, talep eden ve geçmişi dahil tam detay (US-07).</summary>
    Task<Request?> GetDetailAsync(
        int requestId,
        CancellationToken cancellationToken = default);
}
