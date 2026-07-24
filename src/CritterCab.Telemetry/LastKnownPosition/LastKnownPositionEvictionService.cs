using Wolverine;

namespace CritterCab.Telemetry.LastKnownPosition;

// The timer half of the §6.4 eviction sweep, deliberately holding no logic.
//
// Wolverine has NO first-class recurring/scheduled-message primitive: ScheduleAsync is one-shot
// delayed delivery, and PublishMessage<T>().ToLocalQueue() is routing configuration, not a
// scheduler. The idiom — used by Wolverine's own internals, see
// Wolverine/Runtime/Heartbeat/HeartbeatBackgroundService.cs — is a plain .NET BackgroundService
// looping on Task.Delay and calling IMessageBus each tick. Wolverine contributes only the
// IMessageBus call INSIDE the loop; the recurrence itself is vanilla hosting.
//
// All testable behavior lives in EvictStalePositionsHandler. This shell is intentionally not
// covered by tests — there is nothing here to assert that would not be asserting Task.Delay.
public sealed class LastKnownPositionEvictionService(
    IServiceScopeFactory scopeFactory,
    ILogger<LastKnownPositionEvictionService> logger)
    : BackgroundService
{
    // How often to sweep — NOT how stale a document must be to be swept. That threshold is
    // 3 x heartbeatIntervalSeconds and is read from the policy handler-side. Sweeping at roughly
    // the default heartbeat cadence bounds how long an already-stale document lingers to about
    // one extra heartbeat. A constant rather than configuration: v1 has no reason to tune it.
    public static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Delay first: nothing can be stale at host start, so there is no reason to sweep
                // before the first interval elapses.
                await Task.Delay(SweepInterval, stoppingToken);

                // IMessageBus is registered scoped (Wolverine HostBuilderExtensions.cs:232), and
                // this BackgroundService is a singleton — so the bus (and the IDocumentSession the
                // handler resolves) must come from a scope created per tick, never from the root
                // provider. Injecting IMessageBus directly here would fail DI scope validation at
                // startup.
                await using var scope = scopeFactory.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                // InvokeAsync, not PublishAsync: inline and awaited, so a slow sweep applies
                // back-pressure to its own timer instead of overlapping the next tick. (The
                // wolverine-messaging-handlers caution against InvokeAsync is about calling it
                // from inside another handler; this is the carved-out in-process case.)
                await bus.InvokeAsync(new EvictStalePositions(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // normal shutdown, not a failure
            }
            catch (Exception e)
            {
                // A failed sweep must never crash the host — the next tick retries, and a missed
                // sweep only means stale documents linger one interval longer. Same guard
                // Wolverine's own HeartbeatBackgroundService applies.
                logger.LogError(e, "LastKnownPosition eviction sweep failed; retrying next tick.");
            }
        }
    }
}
