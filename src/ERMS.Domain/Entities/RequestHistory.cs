using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

/// <summary>Durum değişikliği geçmişi / audit log (FR-41, FR-42).</summary>
public class RequestHistory : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public int ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;

    public RequestStatus OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; }
}
