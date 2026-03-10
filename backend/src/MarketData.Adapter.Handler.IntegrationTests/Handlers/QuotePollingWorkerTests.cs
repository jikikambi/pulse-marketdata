using MarketData.Adapter.Handler.Handlers;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace MarketData.Adapter.Handler.IntegrationTests.Handlers;

[Collection("Startup")]
public class QuotePollingWorkerTests
{
    private readonly StartupFixture _fixture;

    public QuotePollingWorkerTests(StartupFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PollOnce_ShouldPublishQuote_WhenApiReturnsValidQuote()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();

        var worker = scope.ServiceProvider.GetRequiredService<QuotePollingWorker>();

        await worker.PollOnce(CancellationToken.None);

        var exists = await _fixture.Harness.Published.Any<AlphaVantageQuoteRdm>(x => x.Context.Message.Symbol == "MSFT");

        Assert.True(exists);
    }
}
