using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

/// <summary>
/// Bir talebin "kategorisi" (ör. "İzin", "Masraf", "Donanım") — Admin tarafından yönetilir
/// (Bölüm 5.7). Her tür, kendi onay gerekliliğini taşır; İzin/Masraf'a özgü ek kurallar
/// (tarih/tutar zorunluluğu) ise burada değil, RequestService'te isimle eşleştirilerek
/// uygulanır (bkz. RequestService.LeaveTypeName/ExpenseTypeName — bilinçli bir sınırlama).
/// </summary>
public class RequestType : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>true ise bu türden bir talep önce yönetici onayından geçmeden Approved olamaz (FR-23/24).</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>FR-15: pasif bir tür yeni talep oluştururken seçilemez, ama var olan talepler etkilenmez.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Request> Requests { get; set; } = [];
}
