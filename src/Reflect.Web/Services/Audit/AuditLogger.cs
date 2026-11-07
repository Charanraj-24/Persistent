using System.Collections.Concurrent;
using System.Security.Claims;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces.Repositories;

namespace Reflect.Web.Services.Audit;

/// <summary>
/// Writes the access audit trail. Records who, what and when; never clinical content.
/// Repeated identical events from the same user within a minute (for example re-opening the same tab) are collapsed.
/// </summary>
public class AuditLogger
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastLogged = new();
    private static readonly TimeSpan CollapseWindow = TimeSpan.FromMinutes(1);

    private readonly IAccessAuditRepository _repo;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(IAccessAuditRepository repo, IHttpContextAccessor http)
    {
        _repo = repo;
        _http = http;
    }

    public Task LogAsync(AuditAction action, Guid? clientId = null, string detail = "", bool succeeded = true, CancellationToken ct = default) =>
        LogAsync(action, _http.HttpContext?.User, clientId, detail, succeeded, ct);

    public Task LogAsync(AuditAction action, ClaimsPrincipal user, Guid? clientId, string detail, bool succeeded, CancellationToken ct) =>
        LogActorAsync(action, Guid.TryParse(user?.FindFirstValue("local_user_id"), out var id) ? id : null,
            user?.FindFirstValue("name") ?? user?.Identity?.Name ?? "Anonymous", user?.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            clientId, detail, succeeded, ct);

    /// <summary>For sign-in events, where the request principal is not the actor yet.</summary>
    public async Task LogActorAsync(AuditAction action, Guid? actorId, string actorName, string actorRole, Guid? clientId, string detail, bool succeeded, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var key = $"{actorId}|{action}|{clientId}|{detail}|{succeeded}";
        if (LastLogged.TryGetValue(key, out var last) && now - last < CollapseWindow) return;
        LastLogged[key] = now;

        await _repo.AddAsync(new AccessAuditEntry
        {
            OccurredAt = now,
            ActorUserId = actorId,
            ActorName = actorName.Length > 80 ? actorName[..80] : actorName,
            ActorRole = actorRole,
            Action = action,
            ClientId = clientId,
            Detail = (detail ?? string.Empty).Length > 80 ? detail[..80] : detail ?? string.Empty,
            Succeeded = succeeded
        }, ct);
    }
}
