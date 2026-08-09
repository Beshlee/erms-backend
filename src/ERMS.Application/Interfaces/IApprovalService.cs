using ERMS.Application.DTOs.Approvals;

namespace ERMS.Application.Interfaces;

public interface IApprovalService
{
    /// <summary>FR-32 — yöneticiye bağlı personelin bekleyen taleplerini listeler.</summary>
    Task<List<PendingApprovalItemDto>> GetPendingApprovalsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>FR-33, FR-35, FR-36, FR-37 — talebi onaylar.</summary>
    Task<ApprovalResultDto> ApproveAsync(
        int requestId,
        ApproveRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>FR-33, FR-34, FR-35, FR-36, FR-37 — talebi reddeder (gerekçe zorunlu).</summary>
    Task<ApprovalResultDto> RejectAsync(
        int requestId,
        RejectRequestDto dto,
        CancellationToken cancellationToken = default);
}
