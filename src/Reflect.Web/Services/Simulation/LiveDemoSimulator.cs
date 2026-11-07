using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Services.Ingestion;

namespace Reflect.Web.Services.Simulation;

/// <summary>DEMO ONLY. On/off switch and pacing for the auto-simulator, shared across requests.</summary>
public class LiveDemoState
{
    private readonly object _sync = new();
    public bool Enabled { get; private set; }
    public int IntervalSeconds { get; private set; } = 8;
    public Guid ProviderId { get; private set; }
    public int EventsSent { get; private set; }

    public void Start(Guid providerId, int intervalSeconds)
    {
        lock (_sync) { Enabled = true; ProviderId = providerId; IntervalSeconds = Math.Clamp(intervalSeconds, 3, 120); }
    }

    public void Stop() { lock (_sync) Enabled = false; }
    public void CountEvent() { lock (_sync) EventsSent++; }
}

/// <summary>
/// DEMO ONLY. While enabled, sends a realistic event for a random active client of the provider who started it,
/// every few seconds, through the same <see cref="IngestionService"/> the API uses.
/// </summary>
public class LiveDemoSimulator : BackgroundService
{
    private readonly LiveDemoState _state;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LiveDemoSimulator> _log;
    private readonly Random _rng = new();

    public LiveDemoSimulator(LiveDemoState state, IServiceScopeFactory scopes, ILogger<LiveDemoSimulator> log)
    {
        _state = state;
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var last = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            if (!_state.Enabled || DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(_state.IntervalSeconds)) continue;
            last = DateTimeOffset.UtcNow;
            try
            {
                using var scope = _scopes.CreateScope();
                var clients = (await scope.ServiceProvider.GetRequiredService<IProviderClientRepository>().GetByProviderAsync(_state.ProviderId, stoppingToken))
                    .Where(c => c.Status == ClientRtmStatus.Active).ToList();
                if (clients.Count == 0) continue;
                var client = clients[_rng.Next(clients.Count)];
                var result = await DemoScenarios.RunAsync(DemoScenarios.PickAutomatic(_rng), client,
                    scope.ServiceProvider.GetRequiredService<IngestionService>(), _rng, "simulator", stoppingToken);
                if (result.Ok) _state.CountEvent();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Live demo simulator tick failed.");
            }
        }
    }
}
