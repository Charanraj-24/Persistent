using System.Collections.Concurrent;

namespace Reflect.Web.Services.Assistant;

public sealed record AssistantChatMessage(string Role, string Text, DateTimeOffset At, bool Offline, bool IsError, string Model);

/// <summary>
/// Keeps each provider's assistant conversations on the server: one thread for the caseload and one per client.
/// Server-side (not in the browser) because the text can contain client names and clinical details. The history is only
/// ever returned to the provider who wrote it, and client threads are opened only after the ownership check.
/// DEMO: held in memory and capped; production stores it in the database under the same access rules.
/// </summary>
public class AssistantChatStore
{
    public const int MaxMessagesPerThread = 100;
    private readonly ConcurrentDictionary<string, List<AssistantChatMessage>> _threads = new();

    private static string Key(Guid userId, Guid? clientId) => $"{userId:N}|{(clientId.HasValue ? clientId.Value.ToString("N") : "caseload")}";

    public IReadOnlyList<AssistantChatMessage> Get(Guid userId, Guid? clientId)
    {
        if (!_threads.TryGetValue(Key(userId, clientId), out var list)) return Array.Empty<AssistantChatMessage>();
        lock (list) return list.ToList();
    }

    public void Append(Guid userId, Guid? clientId, params AssistantChatMessage[] messages)
    {
        var list = _threads.GetOrAdd(Key(userId, clientId), _ => new List<AssistantChatMessage>());
        lock (list)
        {
            list.AddRange(messages);
            if (list.Count > MaxMessagesPerThread) list.RemoveRange(0, list.Count - MaxMessagesPerThread);
        }
    }

    public void Clear(Guid userId, Guid? clientId) => _threads.TryRemove(Key(userId, clientId), out _);
}
