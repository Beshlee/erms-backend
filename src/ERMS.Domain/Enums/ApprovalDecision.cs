namespace ERMS.Domain.Enums;

/// <summary>
/// Bir <see cref="ERMS.Domain.Entities.Approval"/> kaydının sonucu (FR-33, FR-35). Talebin
/// kendi durumu (<see cref="RequestStatus"/>) ile karıştırılmamalı: bu enum yalnızca
/// "yönetici ne karar verdi"yi tutar, o kararın talebe nasıl yansıdığını (Approved/Rejected
/// durumuna geçiş) ApprovalService.DecideAsync uygular.
/// </summary>
public enum ApprovalDecision
{
    Approved = 0,
    Rejected = 1
}
