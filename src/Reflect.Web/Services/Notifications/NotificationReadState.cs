using System.Collections.Concurrent;

namespace Reflect.Web.Services.Notifications;

/// <summary>
/// DEMO ONLY: which notification keys each user has marked as read, kept in memory.
/// Production would store this per user in the database.
/// </summary>
public class NotificationReadState
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _read = new();

    public bool IsRead(Guid userId, string key) => _read.TryGetValue(userId, out var set) && set.ContainsKey(key);

    public void MarkRead(Guid userId, IEnumerable<string> keys)
    {
        var set = _read.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        foreach (var k in keys.Where(k => !string.IsNullOrEmpty(k)).Take(500)) set[k] = 0;
    }
}
