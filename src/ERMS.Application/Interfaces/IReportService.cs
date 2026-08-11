using ERMS.Application.DTOs.Admin;

namespace ERMS.Application.Interfaces;

/// <summary>Bölüm 8.3 bonus — admin rapor ekranı.</summary>
public interface IReportService
{
    Task<ReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
