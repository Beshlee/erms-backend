using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

/// <summary>
/// Bir yöneticinin bir talep üzerindeki onay/red kararının kalıcı kaydı (FR-35). Bir talebe
/// birden çok Approval eklenemez çünkü sonuçlanmış (Approved/Rejected) bir talep bir daha
/// karara bağlanamaz (FR-37, ApprovalService.DecideAsync'te uygulanır) — yani pratikte
/// Request:Approval ilişkisi 1:0..1 gibi davranır, ama esneklik için 1:N olarak modellenmiştir.
/// </summary>
public class Approval : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    /// <summary>Kararı veren yönetici — talebi oluşturan Requester ile AYNI kişi olamaz (FR-36).</summary>
    public int ApproverId { get; set; }
    public User Approver { get; set; } = null!;

    public ApprovalDecision Decision { get; set; }

    /// <summary>Reddetmede zorunlu gerekçe, onayda opsiyonel not (FR-34).</summary>
    public string? Comment { get; set; }

    public DateTime DecidedAt { get; set; }
}
