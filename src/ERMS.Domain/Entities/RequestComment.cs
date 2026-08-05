using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

public class RequestComment : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
