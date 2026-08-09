using ERMS.Application.DTOs.Approvals;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize(Roles = nameof(UserRole.Manager))]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    /// <summary>FR-32 — yöneticiye bağlı personelin onay bekleyen taleplerini listeler.</summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingApprovalItemDto>>> GetPending(
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.GetPendingApprovalsAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>FR-33, FR-35..37 — talebi onaylar.</summary>
    [HttpPost("{requestId:int}/approve")]
    public async Task<ActionResult<ApprovalResultDto>> Approve(
        int requestId,
        ApproveRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.ApproveAsync(requestId, request, cancellationToken);

        return Ok(result);
    }

    /// <summary>FR-33, FR-34..37 — talebi reddeder (gerekçe zorunlu).</summary>
    [HttpPost("{requestId:int}/reject")]
    public async Task<ActionResult<ApprovalResultDto>> Reject(
        int requestId,
        RejectRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.RejectAsync(requestId, request, cancellationToken);

        return Ok(result);
    }
}
