using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

public class Approval : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public int ApproverId { get; set; }
    public User Approver { get; set; } = null!;

    public ApprovalDecision Decision { get; set; }

    /// <summary>Reddetmede zorunlu gerekçe, onayda opsiyonel not (FR-34).</summary>
    public string? Comment { get; set; }

    public DateTime DecidedAt { get; set; }
}
