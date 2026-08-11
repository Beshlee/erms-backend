using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

/// <summary>
/// Bir talebe eklenen serbest metin yorumu (FR-38/FR-39) — hem talep sahibi hem ilgili
/// yönetici yazabilir (Approval'dan farkı: bir karar değil, yalnızca iletişimdir; talebin
/// durumunu değiştirmez).
/// </summary>
public class RequestComment : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    /// <summary>Yorumu yazan kişi — talep sahibi veya ilgili yönetici olabilir.</summary>
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
