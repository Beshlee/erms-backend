using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
