using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

/// <summary>Kurumsal departman (ör. "Yazılım", "İnsan Kaynakları") — her User bir departmana bağlıdır.</summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = null!;

    /// <summary>FR-12: departmanlar silinmez, pasife alınır (soft-delete) — kullanan kayıtlar (User) bozulmasın diye.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    /// <summary>Bu departmana bağlı kullanıcılar — EF Core "1 Department → N User" ilişkisinin karşı ucu.</summary>
    public ICollection<User> Users { get; set; } = [];
}
