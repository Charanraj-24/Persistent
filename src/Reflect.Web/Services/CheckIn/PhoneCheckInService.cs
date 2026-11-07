using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Reflect.Web.Services.CheckIn;

public sealed class CheckInJoinSession
{
    public string Code { get; init; } = string.Empty;
    public Guid ProviderUserId { get; init; }
    public Guid ClientId { get; init; }
    public Guid ClientUserId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public int Submissions;
    public bool Stopped { get; set; }
    public bool IsOpen(DateTimeOffset now) => !Stopped && now < ExpiresAt && Submissions < PhoneCheckInService.MaxSubmissionsPerCode;
}

/// <summary>
/// DEMO: short-lived join codes that let phones in the room post a check-in for ONE synthetic client chosen by the presenter.
/// The phone page never sees the client's name or id; it only knows the code. Codes expire, cap submissions,
/// and each device is rate limited. In memory; one active code per provider.
/// </summary>
public class PhoneCheckInService
{
    public const int CodeLength = 6;
    public const int MaxSubmissionsPerCode = 60;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    public const int MaxPerDevice = 3;
    public static readonly TimeSpan DeviceWindow = TimeSpan.FromMinutes(2);
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O, 1/I/L

    private readonly ConcurrentDictionary<string, CheckInJoinSession> _byCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> _deviceHits = new();

    public CheckInJoinSession Start(Guid providerUserId, Guid clientId, Guid clientUserId, DateTimeOffset now)
    {
        foreach (var s in _byCode.Values.Where(s => s.ProviderUserId == providerUserId)) s.Stopped = true;
        foreach (var k in _byCode.Where(kv => kv.Value.ExpiresAt < now.AddHours(-1)).Select(kv => kv.Key).ToList()) _byCode.TryRemove(k, out _);

        string code;
        do code = new string(Enumerable.Range(0, CodeLength).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());
        while (_byCode.ContainsKey(code));

        var session = new CheckInJoinSession { Code = code, ProviderUserId = providerUserId, ClientId = clientId, ClientUserId = clientUserId, ExpiresAt = now + Lifetime };
        _byCode[code] = session;
        return session;
    }

    public void Stop(Guid providerUserId)
    {
        foreach (var s in _byCode.Values.Where(s => s.ProviderUserId == providerUserId)) s.Stopped = true;
    }

    public CheckInJoinSession ActiveFor(Guid providerUserId, DateTimeOffset now) =>
        _byCode.Values.Where(s => s.ProviderUserId == providerUserId && s.IsOpen(now)).OrderByDescending(s => s.ExpiresAt).FirstOrDefault();

    public CheckInJoinSession Find(string code, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != CodeLength) return null;
        return _byCode.TryGetValue(code.Trim(), out var s) && s.IsOpen(now) ? s : null;
    }

    /// <summary>True if this device may submit now (and counts the attempt).</summary>
    public bool TryConsumeDeviceQuota(string deviceKey, DateTimeOffset now)
    {
        var q = _deviceHits.GetOrAdd(deviceKey ?? "unknown", _ => new ConcurrentQueue<DateTimeOffset>());
        lock (q)
        {
            while (q.TryPeek(out var t) && now - t > DeviceWindow) q.TryDequeue(out _);
            if (q.Count >= MaxPerDevice) return false;
            q.Enqueue(now);
            return true;
        }
    }

    public void CountSubmission(CheckInJoinSession s) => Interlocked.Increment(ref s.Submissions);
}
