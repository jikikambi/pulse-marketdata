using FluentAssertions;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Providers;

namespace SignalPulse.MarketData.Application.UnitTests.AI;

public class TemplateReasoningAgentTests
{
    [Fact]
    public async Task GenerateAsync_should_use_mock_provider()
    {
        var provider = new MockQuoteInsightProvider();

        var sut = new TemplateReasoningAgent(provider);

        var result = await sut.GenerateAsync(CreateInput(), null, CancellationToken.None);

        result.Should().NotBeNull();
    }

    private static QuoteInsightInput CreateInput() => new("MSFT", 100m, 2m, 1000, Guid.NewGuid());
}