using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int? ManagerId { get; set; }
    public User? Manager { get; set; }
    public ICollection<User> Employees { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public ICollection<Request> Requests { get; set; } = [];
    public ICollection<Approval> Approvals { get; set; } = [];
    public ICollection<RequestComment> Comments { get; set; } = [];
}
