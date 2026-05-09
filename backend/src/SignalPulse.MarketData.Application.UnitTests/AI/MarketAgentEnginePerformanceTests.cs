using FakeItEasy;
using FluentAssertions;
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
using System.Diagnostics;
using System.Globalization;

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

    [Fact]
    public async Task RunAsync_WithInvalidPrice_ShouldReturnQuickly()
    {
        // Arrange
        var input = new QuoteInsightInput(
            Symbol: "AAPL",
            Price: -1m,
            ChangePercent: 2.5m,
            Volume: 1_000_000,
            CorrelationId: Guid.NewGuid());

        var sw = Stopwatch.StartNew();

        var engine = new MarketAgentEngine(_kernelInvoker, _quotePlugin, _store, _logger);

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        sw.Stop();

        // Assert
        result.Should().NotBeNull();

        result.Sentiment.Should().Be(SentimentType.Neutral);

        result.Direction.Should().Be(DirectionType.Sideways);

        result.Volatility.Should().Be(VolatilityType.High);

        result.Rationale.Should().Contain("invalid_market_data");

        sw.ElapsedMilliseconds.Should().BeLessThan(500);

        A.CallTo(() => _kernelInvoker.InvokeAsync(A<string>._, A<KernelArguments>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RunAsync_WithPlannerTimeout_ShouldReturnFallback()
    {
        // Arrange
        var input = new QuoteInsightInput(
            Symbol: "MSFT",
            Price: 420m,
            ChangePercent: 1.2m,
            Volume: 2_000_000,
            CorrelationId: Guid.NewGuid());

        var cacheKey = $"MSFT:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        var engine = new MarketAgentEngine(_kernelInvoker, _quotePlugin, _store, _logger);

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        // Simulate planner timeout
        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        result.Sentiment.Should().Be(SentimentType.Neutral);

        result.Direction.Should().Be(DirectionType.Sideways);

        result.Volatility.Should().Be(VolatilityType.Low);

        result.Rationale.Should().Contain("planner_timeout");

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.ReasonerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RunAsync_EndToEnd_ShouldCompleteWithin3Seconds()
    {
        // Arrange
        var input = new QuoteInsightInput(
            Symbol: "NVDA",
            Price: 950m,
            ChangePercent: 4.2m,
            Volume: 5_000_000,
            CorrelationId: Guid.NewGuid());

        var cacheKey = $"NVDA:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        var plannerJson = """
        {
            "needTool": false,
            "tool": null,
            "confidence": 0.91,
            "reason": "sufficient data"
        }
        """;

        var reasonerJson = """
        {
            "sentiment": "bullish",
            "direction": "up",
            "volatility": "medium",
            "rationale": "strong positive momentum"
        }
        """;

        var engine = new MarketAgentEngine(_kernelInvoker, _quotePlugin, _store, _logger);

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .Returns(plannerJson);

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.ReasonerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        var sw = Stopwatch.StartNew();

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        sw.Stop();

        // Assert
        result.Should().NotBeNull();

        result.Sentiment.Should().Be(SentimentType.Bullish);

        result.Direction.Should().Be(DirectionType.Up);

        result.Volatility.Should().Be(VolatilityType.Medium);

        result.Rationale.Should().Contain("positive momentum");

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _kernelInvoker.InvokeAsync(AgentConstants.ReasonerFunction, A<KernelArguments>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _store.SetPlanCacheAsync(cacheKey, A<string>._))
            .MustHaveHappenedOnceExactly();
    }
}