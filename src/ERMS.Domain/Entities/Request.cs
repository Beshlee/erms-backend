using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

/// <summary>
/// Uygulamanın merkezindeki entity — bir çalışanın oluşturduğu izin/masraf/vb. talebi.
/// Diğer her şey (Approval, RequestComment, RequestAttachment, RequestHistory) bir Request'e
/// bağlıdır; bu yüzden Request'in EF Core Configuration'ı (RequestConfiguration) ve
/// RequestQueryRepository, projedeki en çok Include/join içeren yerlerdir.
/// </summary>
public class Request : BaseEntity
{
    /// <summary>Hangi türden (İzin/Masraf/...) bir talep olduğu — bkz. <see cref="RequestType"/>.</summary>
    public int RequestTypeId { get; set; }
    public RequestType RequestType { get; set; } = null!;

    /// <summary>Talebi oluşturan çalışan. "Requester" adı bilinçli seçildi — "Owner" da olabilirdi
    /// ama kod genelinde "kim onayladı" (Approver) ile karışmasın diye ayrıştırıldı.</summary>
    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public RequestStatus Status { get; set; }
    public RequestPriority Priority { get; set; }

    /// <summary>İzin türü talepler için tarih aralığı (FR-18) — diğer türlerde null kalır.</summary>
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Masraf türü talepler için tutar (FR-19) — diğer türlerde null kalır.</summary>
    public decimal? Amount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Aşağıdaki dört koleksiyon, EF Core'un "1 Request → N X" ilişkilerinin Request tarafındaki
    // ucudur — RequestQueryRepository.GetDetailAsync bunları Include ile birlikte yükler ki
    // Talep Detayı ekranı tek bir sorguda tüm geçmiş/yorum/ek bilgisini gösterebilsin.
    public ICollection<Approval> Approvals { get; set; } = [];
    public ICollection<RequestComment> Comments { get; set; } = [];
    public ICollection<RequestAttachment> Attachments { get; set; } = [];
    public ICollection<RequestHistory> History { get; set; } = [];
}
