// STAND-IN: exact contract from the Reflect XR tech spec (Part 2, "Backend conventions").
// Delete this file on merge; the real Reflect.Domain type takes over.
namespace Reflect.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message) { }
}
