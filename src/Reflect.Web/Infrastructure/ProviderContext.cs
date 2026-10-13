using Reflect.Domain.Common;

namespace Reflect.Web.Infrastructure;

/// <summary>Who is signed in and what "now" is. Passed into ViewModels so they stay testable.</summary>
public sealed class ProviderContext
{
    public ProviderContext(ICurrentUserService currentUser, TimeProvider clock)
    {
        ProviderId = currentUser.UserId ?? throw new EntityNotFoundException("No signed-in provider.");
        NowUtc = clock.GetUtcNow();
        Today = NowUtc.ToEasternDate();
    }

    public Guid ProviderId { get; }
    public DateTimeOffset NowUtc { get; }
    public DateOnly Today { get; }
}
