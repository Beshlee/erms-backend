using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;

namespace ERMS.Application.Services;

/// <summary>
/// Bonus — admin rapor ekranı. Ham GroupBy/Count sonuçlarını (IReportQueryRepository'den)
/// alıp dashboard'un ihtiyaç duyduğu DTO şekline (ör. hiç kaydı olmayan bir durumu bile
/// 0 ile göstermek) dönüştürür — SQL'in kendisiyle ilgilenmez, o Infrastructure'dadır.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly IReportQueryRepository _reportQueryRepository;

    public ReportService(IReportQueryRepository reportQueryRepository)
    {
        _reportQueryRepository = reportQueryRepository;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var total = await _reportQueryRepository.GetTotalRequestCountAsync(cancellationToken);
        var byStatusRaw = await _reportQueryRepository.GetCountsByStatusAsync(cancellationToken);
        var byTypeRaw = await _reportQueryRepository.GetCountsByTypeAsync(cancellationToken);
        var byDepartmentRaw = await _reportQueryRepository.GetCountsByDepartmentAsync(cancellationToken);

        // Dashboard'da tutarlı bir görünüm için tüm durumlar, hiç talebi olmasa bile
        // 0 sayısıyla gösterilir (aksi halde ör. hiç "Cancelled" yoksa çubuk hiç görünmez).
        var statusLookup = byStatusRaw.ToDictionary(x => x.Status, x => x.Count);
        var byStatus = Enum.GetValues<RequestStatus>()
            .Select(status => new StatusCountDto(status.ToString(), statusLookup.GetValueOrDefault(status)))
            .ToList();

        var byType = byTypeRaw
            .OrderByDescending(x => x.Count)
            .Select(x => new TypeCountDto(x.TypeName, x.Count))
            .ToList();

        var byDepartment = byDepartmentRaw
            .OrderByDescending(x => x.Count)
            .Select(x => new DepartmentCountDto(x.DepartmentName, x.Count))
            .ToList();

        return new ReportSummaryDto(total, byStatus, byType, byDepartment);
    }
}
