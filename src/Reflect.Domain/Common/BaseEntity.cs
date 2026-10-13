namespace Reflect.Domain.Common;

// Matches the Reflect XR BaseEntity contract exactly (tech spec, Part 2 "Backend conventions").
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DateDeleted { get; set; }
}
