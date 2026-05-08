using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;

namespace SignalPulse.MarketData.Application.UnitTests.AI;

public sealed class MarketAgentEnginePerformanceTests
{
    private readonly IKernelInvoker _kernelInvoker = A.Fake<IKernelInvoker>();
    private readonly QuoteInfoPlugin _quotePlugin = A.Fake<QuoteInfoPlugin>();
    private readonly IAgentStateStore _store = A.Fake<IAgentStateStore>();

    private readonly ILogger<MarketAgentEngine> _logger = NullLogger<MarketAgentEngine>.Instance;

    [Fact]
    public async Task RunAsync_WithSameSymbolAndChangePercent_ShouldUsePlannerCache()
    {
        // Arrange

        var input1 = new QuoteInsightInput("AAPL", 150m, 2.5m, 1_000_000, Guid.NewGuid());

        var input2 = new QuoteInsightInput("AAPL", 151m, 2.5m, 1_100_000, Guid.NewGuid());

        var engine = new MarketAgentEngine(_kernelInvoker, _quotePlugin, _store, _logger);

        var plannerJson = """
        {
            "needTool": false,
            "tool": null,
            "confidence": 0.9,
            "reason": "sufficient data"
        }
        """;

        var reasonerJson = """
        {
            "sentiment": "bullish",
            "direction": "up",
            "volatility": "medium",
            "rationale": "positive momentum"
        }
        """;

        // Cache:
        // First = miss
        // Second = hit

        A.CallTo(() => _store.GetPlanCacheAsync("AAPL:2.5"))
            .ReturnsNextFromSequence(null, plannerJson);

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .Returns(plannerJson);

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.ReasonerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        // Act

        var result1 = await engine.RunAsync(input1, CancellationToken.None);

        var result2 = await engine.RunAsync(input2, CancellationToken.None);

        // Assert

        Assert.NotNull(result1);
        Assert.NotNull(result2);

        Assert.Equal(SentimentType.Bullish, result1.Sentiment);
        Assert.Equal(SentimentType.Bullish, result2.Sentiment);

        A.CallTo(() => _store.SetPlanCacheAsync("AAPL:2.5", A<string>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _store.GetPlanCacheAsync("AAPL:2.5"))
            .MustHaveHappenedTwiceExactly();

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.ReasonerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }
}