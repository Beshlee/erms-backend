using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers.Admin;

/// <summary>Bölüm 8.3 bonus — admin rapor ekranı (durum/tür/departman bazlı talep sayıları).</summary>
[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSummaryAsync(cancellationToken);

        return Ok(result);
    }
}
