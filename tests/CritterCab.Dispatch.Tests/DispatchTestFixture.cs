using Alba;
using CritterCab.Dispatch.FareQuoting;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CritterCab.Dispatch.Tests;

public class DispatchTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .Build();

    public IAlbaHost Host { get; private set; } = null!;

    // Per-test overrides. Default values keep slice 5.1 and slice 5.2 happy-path
    // tests passing without arrangement. Failure-path tests assign these in their
    // arrange phase. The DI registration in InitializeAsync wraps both behind
    // forwarders so mutations after host construction take effect on the next
    // handler resolution.
    public IPricingClient PricingClient { get; set; } = new PricingClientStub();
    public FareQuoteRetryPolicy RetryPolicy { get; set; } =
        new FareQuoteRetryPolicy(MaxAttempts: 3, Cooldown: TimeSpan.FromMilliseconds(10));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_dispatch", _postgres.GetConnectionString());

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPricingClient>(_ => new ForwardingPricingClient(() => PricingClient));
                services.AddSingleton(_ => RetryPolicy);
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // Stable singleton that defers every call to the fixture's current
    // PricingClient — lets tests swap stubs after host construction without
    // rebuilding the Alba host.
    private sealed class ForwardingPricingClient(Func<IPricingClient> inner) : IPricingClient
    {
        public Task<GetFareQuoteResponse> GetFareQuoteAsync(
            GetFareQuoteRequest request,
            CancellationToken cancellationToken = default) =>
            inner().GetFareQuoteAsync(request, cancellationToken);
    }
}

[CollectionDefinition("Dispatch")]
public class DispatchCollection : ICollectionFixture<DispatchTestFixture>;
