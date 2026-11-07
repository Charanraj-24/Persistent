// STAND-IN: exact contract from the Reflect XR tech spec (Part 2, "Backend conventions").
// Delete this file on merge; the real Reflect.Domain type takes over.
namespace Reflect.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DateDeleted { get; set; }
}
