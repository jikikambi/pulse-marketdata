using FluentAssertions;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Persistence;

[Collection("Startup")]
public class ForexInsightRepositoryTests(StartupFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Upsert_And_GetById_Should_Work()
    {
        await CleanDatabase();

        await using var session = Store.LightweightSession();
        var repo = new ForexInsightRepository(session);

        var model = new ForexInsightReadModel
        {
            Id = Guid.NewGuid(),
            FromSymbol = "EUR",
            ToSymbol = "USD",
            Open = 1.1m,
            High = 1.2m,
            Low = 1.0m,
            Close = 1.15m,
            ForexDate = DateTimeOffset.UtcNow,
            Sentiment = "Neutral",
            Direction = "Sideways",
            Volatility = "Low",
            Rationale = "Test",
            ObservedAt = DateTimeOffset.UtcNow
        };

        await repo.UpsertAsync(model);

        var result = await repo.GetByIdAsync(model.Id);

        result.Should().NotBeNull();
        result!.FromSymbol.Should().Be("EUR");
    }
}