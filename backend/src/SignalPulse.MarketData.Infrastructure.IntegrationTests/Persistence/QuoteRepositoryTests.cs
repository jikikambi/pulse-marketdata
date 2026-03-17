using FluentAssertions;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Persistence;

[Collection("Startup")]
public class QuoteRepositoryTests(StartupFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Upsert_And_GetById_Should_Work()
    {
        await CleanDatabase();

        await using var session = Store.LightweightSession();
        var repo = new QuoteRepository(session);

        var model = new QuoteReadModel
        {
            Id = Guid.NewGuid(),
            Symbol = "AAPL",
            Price = 150,
            ChangePercent = 1.5m,
            Timestamp = DateTime.UtcNow
        };

        await repo.UpsertAsync(model);

        var result = await repo.GetByIdAsync(model.Id);

        result.Should().NotBeNull();
        result!.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetAll_Should_Return_All_Items()
    {
        await CleanDatabase();

        await using var session = Store.LightweightSession();
        var repo = new QuoteRepository(session);

        var items = new[]
        {
            new QuoteReadModel { Id = Guid.NewGuid(), Symbol = "AAPL", Price = 1, Timestamp = DateTime.UtcNow },
            new QuoteReadModel { Id = Guid.NewGuid(), Symbol = "MSFT", Price = 2, Timestamp = DateTime.UtcNow }
        };

        foreach (var item in items)
            session.Store(item);

        await session.SaveChangesAsync();

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(2);
    }
}