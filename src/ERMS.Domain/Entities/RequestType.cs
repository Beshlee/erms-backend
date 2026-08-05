using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

public class RequestType : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Request> Requests { get; set; } = [];
}
