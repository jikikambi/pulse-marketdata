using FluentAssertions;
using SignalPulse.MarketData.Domain.Forex;
using SignalPulse.MarketData.Domain.Quotes;
using SignalPulse.MarketData.Infrastructure.EventStore;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.EventStore;

[Collection("Startup")]
public class MartenAggregateRepositoryQuoteTests(StartupFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Persist_And_Load_QuoteAggregate_Should_Work()
    {
        await CleanDatabase();

        var symbol = "AAPL";

        // Create aggregate
        var aggregate = QuoteAggregate.Create(symbol, 150m, 1.5m);

        // Persist
        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);
            await repo.PersistAsync(aggregate, CancellationToken.None);
        }

        // Load (new session!)
        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);

            var loaded = await repo.LoadAsync<QuoteAggregate>(aggregate.Id, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Symbol.Should().Be("AAPL");
            loaded.Price.Should().Be(150m);
            loaded.ChangePercent.Should().Be(1.5m);
        }
    }

    [Fact]
    public async Task Persist_Update_And_Load_Should_Rehydrate_Correctly()
    {
        await CleanDatabase();

        var aggregate = QuoteAggregate.Create("AAPL", 100m, 1m);

        // First persist
        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);
            await repo.PersistAsync(aggregate, CancellationToken.None);
        }

        // Update + persist again
        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);

            var loaded = await repo.LoadAsync<QuoteAggregate>(aggregate.Id, CancellationToken.None);
            loaded!.Update("AAPL", 200m, 2m);

            await repo.PersistAsync(loaded, CancellationToken.None);
        }

        // Final load
        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);

            var final = await repo.LoadAsync<QuoteAggregate>(aggregate.Id, CancellationToken.None);

            final!.Price.Should().Be(200m);
            final.ChangePercent.Should().Be(2m);
        }
    }

    [Fact]
    public async Task Persist_And_Load_ForexAggregate_Should_Work()
    {
        await CleanDatabase();

        var fx = ForexAggregate.Create("EUR", "USD", 1.1m, 1.2m, 1.0m, 1.15m, DateTimeOffset.UtcNow);

        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);
            await repo.PersistAsync(fx, CancellationToken.None);
        }

        await using (var session = Store.LightweightSession())
        {
            var repo = new MartenAggregateRepository(session);

            var loaded = await repo.LoadAsync<ForexAggregate>(fx.Id, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.FromSymbol.Should().Be("EUR");
            loaded.ToSymbol.Should().Be("USD");
            loaded.Close.Should().Be(1.15m);
        }
    }
}