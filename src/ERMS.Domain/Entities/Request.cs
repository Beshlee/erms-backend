using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

public class Request : BaseEntity
{
    public int RequestTypeId { get; set; }
    public RequestType RequestType { get; set; } = null!;

    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public RequestStatus Status { get; set; }
    public RequestPriority Priority { get; set; }

    /// <summary>İzin türü talepler için tarih aralığı (FR-18).</summary>
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Masraf türü talepler için tutar (FR-19).</summary>
    public decimal? Amount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Approval> Approvals { get; set; } = [];
    public ICollection<RequestComment> Comments { get; set; } = [];
    public ICollection<RequestAttachment> Attachments { get; set; } = [];
    public ICollection<RequestHistory> History { get; set; } = [];
}
