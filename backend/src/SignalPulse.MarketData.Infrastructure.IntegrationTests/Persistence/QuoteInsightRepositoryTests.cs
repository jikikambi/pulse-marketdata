using FluentAssertions;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Persistence;

[Collection("Startup")]
public class QuoteInsightRepositoryTests(StartupFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Upsert_And_GetById_Should_Work()
    {
        await CleanDatabase();

        await using var session = Store.LightweightSession();
        var repo = new QuoteInsightRepository(session);

        var model = new QuoteInsightReadModel
        {
            Id = Guid.NewGuid(),
            Symbol = "BTC",
            Price = 50000,
            Sentiment = "Bullish",
            Direction = "Up",
            Volatility = "High",
            Rationale = "Test",
            ObservedAt = DateTimeOffset.UtcNow
        };

        await repo.UpsertAsync(model);

        var result = await repo.GetByIdAsync(model.Id);

        result.Should().NotBeNull();
        result!.Sentiment.Should().Be("Bullish");
    }
}