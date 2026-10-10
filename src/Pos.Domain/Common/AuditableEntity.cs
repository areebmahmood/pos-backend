namespace Pos.Domain.Common;

/// <summary>
/// Entity whose creation and modification timestamps are stamped by the persistence layer.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
