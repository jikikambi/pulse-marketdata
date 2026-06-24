using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Timeout;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;
using System.Diagnostics;
using System.Globalization;

namespace SignalPulse.MarketData.Application.UnitTests.AI;

public sealed class MarketAgentEnginePerformanceTests
{
    private readonly IKernelInvoker _kernelInvoker = A.Fake<IKernelInvoker>();
    private readonly IQuoteInfoTool _quoteTool = A.Fake<IQuoteInfoTool>();
    private readonly IAgentStateStore _store = A.Fake<IAgentStateStore>();

    private readonly IRiskAgent _riskAgent = A.Fake<IRiskAgent>();
    private readonly IValidatorAgent _validatorAgent = A.Fake<IValidatorAgent>();
    private readonly IConfidenceScoringAgent _confidenceScoringAgent = A.Fake<IConfidenceScoringAgent>();
    private readonly IFinalDecisionAgent _finalDecisionAgent = A.Fake<IFinalDecisionAgent>();
    private readonly IWorkflowEventSink _eventSink = A.Fake<IWorkflowEventSink>();
    private readonly IAiPolicyRegistry _policyRegistry = A.Fake<IAiPolicyRegistry>();
    private readonly IMarketStageOrchestrator _orchestrator = A.Fake<IMarketStageOrchestrator>();
    private readonly IReasoningAgentResolver _reasoningAgentResolver = A.Fake<IReasoningAgentResolver>();
    private readonly IReasoningAgent _primaryReasoningAgent = A.Fake<IReasoningAgent>();
    private readonly IReasoningAgent _fallbackReasoningAgent = A.Fake<IReasoningAgent>();
    private readonly IPlannerAgentResolver _plannerAgentResolver = A.Fake<IPlannerAgentResolver>();
    private readonly IPlannerExecutionAgent _primaryPlannerAgent = A.Fake<IPlannerExecutionAgent>();
    private readonly IPlannerExecutionAgent _fallbackPlannerAgent = A.Fake<IPlannerExecutionAgent>();
    private static readonly IOptions<MarketAgentOptions> options = Options.Create(new MarketAgentOptions { MaxParallelStages = 3 });

    private readonly IMarketStageScheduler _scheduler = new MarketStageScheduler(options);

    private readonly ILogger<MarketAgentEngine> _logger = NullLogger<MarketAgentEngine>.Instance;

    [Fact]
    public async Task RunAsync_WithSameSymbolAndChangePercent_ShouldUsePlannerCache()
    {
        // Arrange
        var input1 = new QuoteInsightInput("AAPL", 150m, 2.5m, 1_000_000, Guid.NewGuid());

        var input2 = new QuoteInsightInput("AAPL", 151m, 2.5m, 1_100_000, Guid.NewGuid());

        const string cacheKey = "AAPL:2.5";

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

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .ReturnsNextFromSequence(null, plannerJson);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
            .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
           .Returns(reasonerJson);

        SetupSuccessfulGovernancePipeline(input1);
        SetupSuccessfulGovernancePipeline(input2);

        // Act
        var result1 = await engine.RunAsync(input1, CancellationToken.None);

        var result2 = await engine.RunAsync(input2, CancellationToken.None);

        // Assert
        result1.Sentiment.Should().Be(SentimentType.Bullish);
        result2.Sentiment.Should().Be(SentimentType.Bullish);

        A.CallTo(() => _store.SetPlanCacheAsync(cacheKey, A<string>._!))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .MustHaveHappenedTwiceExactly();

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task RunAsync_WithInvalidPrice_ShouldReturnQuickly()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", -1m, 2.5m, 1_000_000, Guid.NewGuid());

        var engine = CreateEngine();

        var sw = Stopwatch.StartNew();

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
        var input = new QuoteInsightInput("MSFT", 420m, 1.2m, 2_000_000, Guid.NewGuid());

        var cacheKey = $"MSFT:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        var plannerJson = """
            
             {
                "needTool": false,
                "tool": null,
                "confidence": 0.90,
                "reason": "fallback planner"
              }            
            """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .ThrowsAsync(new TimeoutRejectedException());

        A.CallTo(() => _fallbackPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
            .Returns(plannerJson);

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.Direction.Should().Be(DirectionType.Sideways);
        result.Volatility.Should().Be(VolatilityType.Low);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fallbackPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RunAsync_WhenPlannerRequestsUnauthorizedTool_ShouldReturnSafeFallback()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", 180m, 3.2m, 1_500_000, Guid.NewGuid());

        var cacheKey = $"AAPL:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        var plannerJson = """
        {
             "needTool": true,
             "tool": "HackSystemAsync",
             "confidence": 0.95,
             "reason": "requires external access"
        }
        """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.Direction.Should().Be(DirectionType.Sideways);
        result.Volatility.Should().Be(VolatilityType.Low);

        result.Rationale.Should().Contain("unauthorized_tool_request");

        A.CallTo(() => _quoteTool.GetQuoteContextAsync(A<string>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RunAsync_WhenToolReturnsNull_ShouldReturnSafeFallback()
    {
        // Arrange
        var input = new QuoteInsightInput("MSFT", 420m, 2.1m, 2_000_000, Guid.NewGuid());

        var cacheKey = $"MSFT:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        var plannerJson = """
        {
            "needTool": true,
            "tool": "GetQuoteContextAsync",
            "confidence": 0.91,
            "reason": "historical comparison required"
        }
        """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _quoteTool.GetQuoteContextAsync("MSFT"))
            .Returns(Task.FromResult<QuoteContextResult?>(null));

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.Direction.Should().Be(DirectionType.Sideways);
        result.Volatility.Should().Be(VolatilityType.Low);

        result.Rationale.Should().Contain("missing_tool_data");

        A.CallTo(() => _quoteTool.GetQuoteContextAsync("MSFT"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RunAsync_WhenPlannerRequestsTool_ShouldInvokeToolAndPassContextToReasoner()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", 150m, 2.5m, 1_000_000, Guid.NewGuid());

        const string cacheKey = "AAPL:2.5";

        var plannerJson = """
        {
            "needTool": true,
            "tool": "GetQuoteContextAsync",
            "confidence": 0.91,
            "reason": "historical comparison required"
        }
        """;

        var reasonerJson = """
        {
            "sentiment": "bullish",
            "direction": "up",
            "volatility": "medium",
            "rationale": "historical trend supports upward momentum"
        }
        """;

        var toolResult = new QuoteContextResult
        {
            Price = 145m,
            ChangePercent = 1.2m,
            Source = "cache"
        };

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _quoteTool.GetQuoteContextAsync("AAPL"))
            .Returns(toolResult);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>
            .That.Matches(x => x != null && x.Contains("145")), A<CancellationToken>._))
            .Returns(reasonerJson);

        SetupSuccessfulGovernancePipeline(input);

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Bullish);
        result.Direction.Should().Be(DirectionType.Up);
        result.Volatility.Should().Be(VolatilityType.Medium);

        result.Rationale.Should().Contain("historical");

        A.CallTo(() => _quoteTool.GetQuoteContextAsync("AAPL"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RunAsync_WhenValidatorFails_ShouldReturnSafeFallback()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", 180m, 1.5m, 1_000_000, Guid.NewGuid());

        const string cacheKey = "AAPL:1.5";

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
            "rationale": "healthy upward momentum"
        }
        """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        A.CallTo(() => _validatorAgent.ValidateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new ValidationResult(false, "invalid rationale", ValidationSeverity.High));

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.Direction.Should().Be(DirectionType.Sideways);

        result.Rationale.Should().Contain("validation_failed");

        A.CallTo(() => _riskAgent.EvaluateAsync(A<QuoteInsightInput>._, A<AIInsightResult>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RunAsync_WhenRiskAgentRejectsInsight_ShouldReturnSafeFallback()
    {
        // Arrange
        var input = new QuoteInsightInput("TSLA", 250m, 12.5m, 5_000_000, Guid.NewGuid());

        const string cacheKey = "TSLA:12.5";

        var plannerJson = """
        {
            "needTool": false,
            "tool": null,
            "confidence": 0.95,
            "reason": "strong signal"
        }
        """;

        var reasonerJson = """
        {
            "sentiment": "bullish",
            "direction": "up",
            "volatility": "high",
            "rationale": "extreme breakout momentum"
        }
        """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        A.CallTo(() => _validatorAgent.ValidateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new ValidationResult(true, "passed", ValidationSeverity.Low));

        A.CallTo(() => _riskAgent.EvaluateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new RiskAssessmentResult(true, "High volatility with extreme price movement", RiskLevel.High));

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.Direction.Should().Be(DirectionType.Sideways);
        result.Volatility.Should().Be(VolatilityType.Low);

        result.Rationale.Should().Contain("risk_threshold_exceeded");
    }

    [Fact]
    public async Task RunAsync_WhenConfidenceIsLow_ShouldRejectDecision()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", 180m, 1.5m, 1_000_000, Guid.NewGuid());

        const string cacheKey = "AAPL:1.5";

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
            "rationale": "healthy upward momentum"
        }
        """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        A.CallTo(() => _validatorAgent.ValidateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new ValidationResult(true, "passed", ValidationSeverity.Low));

        A.CallTo(() => _riskAgent.EvaluateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new RiskAssessmentResult(false, "acceptable", RiskLevel.Low));

        A.CallTo(() => _confidenceScoringAgent.ScoreAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(new ConfidenceScoreResult(0.31, ConfidenceLevel.Low, "weak confidence"));

        A.CallTo(() => _finalDecisionAgent.DecideAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(new FinalDecisionResult(DecisionOutcome.Rejected, "confidence too low"));

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.Direction.Should().Be(DirectionType.Sideways);

        result.Rationale.Should().Contain("decision_rejected");
    }

    [Fact]
    public async Task RunAsync_WhenRiskAgentApprovesInsight_ShouldReturnReasonerResult()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", 180m, 1.5m, 1_000_000, Guid.NewGuid());

        const string cacheKey = "AAPL:1.5";

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
            "rationale": "healthy upward momentum"
        }
        """;

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        SetupSuccessfulGovernancePipeline(input);

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Sentiment.Should().Be(SentimentType.Bullish);
        result.Direction.Should().Be(DirectionType.Up);
        result.Volatility.Should().Be(VolatilityType.Medium);

        result.Rationale.Should().Contain("upward momentum");
    }

    [Fact]
    public async Task RunAsync_ShouldRecordStageExecutionMetadata()
    {
        // Arrange
        var input = new QuoteInsightInput("NVDA", 950m, 4.2m, 5_000_000, Guid.NewGuid());

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

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        SetupSuccessfulGovernancePipeline(input);

        MarketAgentState? persistedState = null;

        A.CallTo(() => _store.SetAsync(A<string>._!, A<MarketAgentState>._))
            .Invokes(call => persistedState = call.GetArgument<MarketAgentState>(1));

        // Act
        await engine.RunAsync(input, CancellationToken.None);

        // Assert
        persistedState.Should().NotBeNull();

        persistedState!.StageResults.Should().NotBeEmpty();

        persistedState.StageResults.Should()
            .Contain(x => x.Stage == MarketAgentStage.Planning.ToString());

        persistedState.StageResults.Should()
            .Contain(x => x.Stage == MarketAgentStage.Reasoning.ToString());

        persistedState.StageResults.Should()
            .Contain(x => x.Stage == MarketAgentStage.Validation.ToString());

        persistedState.StageResults.Should()
            .Contain(x => x.Stage == MarketAgentStage.RiskEvaluation.ToString());

        persistedState.StageResults.Should()
            .Contain(x => x.Stage == MarketAgentStage.Scoring.ToString());

        persistedState.StageResults.Should()
            .Contain(x => x.Stage == MarketAgentStage.Decision.ToString());

        persistedState.StageResults.Should()
            .OnlyContain(x => x.DurationMs >= 0 && x.CompletedAt >= x.StartedAt);
    }

    [Fact]
    public async Task RunAsync_EndToEnd_ShouldCompleteWithin3Seconds()
    {
        // Arrange
        var input = new QuoteInsightInput("NVDA", 950m, 4.2m, 5_000_000, Guid.NewGuid());

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

        var engine = CreateEngine();

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);

        A.CallTo(() => _reasoningAgentResolver.GetPrimary())
            .Returns(_primaryReasoningAgent);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);

        SetupSuccessfulGovernancePipeline(input);

        var sw = Stopwatch.StartNew();

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        sw.Stop();

        // Assert
        result.Sentiment.Should().Be(SentimentType.Bullish);
        result.Direction.Should().Be(DirectionType.Up);
        result.Volatility.Should().Be(VolatilityType.Medium);

        result.Rationale.Should().Contain("positive momentum");

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task RunAsync_WhenValidationFails_ShouldTerminatePipeline()
    {
        // Arrange
        var input = new QuoteInsightInput("AAPL", 150m, 2m, 1_000_000, Guid.NewGuid());

        SetupPlannerAndReasoner();

        A.CallTo(() => _validatorAgent.ValidateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new ValidationResult(false, "invalid", ValidationSeverity.High));

        var engine = CreateEngine();

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Rationale.Should().Contain("validation_failed");

        A.CallTo(() => _riskAgent.EvaluateAsync(A<QuoteInsightInput>._, A<AIInsightResult>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RunAsync_ShouldPersistWorkflowState()
    {
        // Arrange
        var input = CreateValidInput();

        SetupSuccessfulPipeline(input);

        var engine = CreateEngine();

        // Act
        await engine.RunAsync(input, CancellationToken.None);

        // Assert
        A.CallTo(() => _store.SetAsync(A<string>._, A<MarketAgentState>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RunAsync_WhenDecisionRejected_ShouldReturnFallback()
    {
        // Arrange
        var input = CreateValidInput();

        SetupPlannerAndReasoner();

        SetupSuccessfulValidation(input);

        A.CallTo(() => _finalDecisionAgent.DecideAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(new FinalDecisionResult(DecisionOutcome.Rejected, "low confidence"));

        var engine = CreateEngine();

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Rationale.Should().Contain("decision_rejected");
    }

    [Fact]
    public async Task RunAsync_WhenPlannerReturnsInvalidJson_ShouldReturnFallback()
    {
        // Arrange
        var input = CreateValidInput();

        var cacheKey = "AAPL:2";

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
          .Returns("INVALID_JSON");

        var engine = CreateEngine();

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Rationale.Should().Contain("planner_deserialization_failed");
    }

    [Fact]
    public async Task RunAsync_WhenPlanConfidenceTooLow_ShouldReturnFallback()
    {
        // Arrange
        var input = CreateValidInput();

        var plannerJson = """
        {
            "needTool": false,
            "tool": null,
            "confidence": 0.2,
            "reason": "weak signal"
        }
        """;

        SetupPlanner(plannerJson);

        var engine = CreateEngine();

        // Act
        var result = await engine.RunAsync(input, CancellationToken.None);

        // Assert
        result.Rationale.Should().Contain("low_confidence");
    }

    [Fact]
    public async Task RunAsync_ShouldEmitWorkflowEvents()
    {
        // Arrange
        var input = CreateValidInput();

        SetupSuccessfulPipeline(input);

        var engine = CreateEngine();

        // Act
        await engine.RunAsync(input, CancellationToken.None);

        // Assert

        A.CallTo(() => _eventSink.WriteAsync(A<WorkflowEvent>.That.Matches(x =>
        x.Stage == MarketAgentStage.Planning.ToString() &&
        x.EventType == "planner_started"), A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _eventSink.WriteAsync(A<WorkflowEvent>.That.Matches(x =>
        x.Stage == MarketAgentStage.Reasoning.ToString() &&
        x.EventType == "stage_completed"), A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _eventSink.WriteAsync(A<WorkflowEvent>.That.Matches(x =>
        x.Stage == MarketAgentStage.Decision.ToString() &&
        x.EventType == "decision_approved"), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task RunAsync_WhenValidationFails_ShouldEmitTerminationEvent()
    {
        // Arrange
        var input = CreateValidInput();

        SetupPlannerAndReasoner(input);

        A.CallTo(() => _validatorAgent.ValidateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new ValidationResult(false, "invalid", ValidationSeverity.High));

        var engine = CreateEngine();

        // Act
        await engine.RunAsync(input, CancellationToken.None);

        // Assert
        A.CallTo(() => _eventSink.WriteAsync(A<WorkflowEvent>.That.Matches(x =>
        x.Stage == "workflow" &&
        x.EventType == "workflow_terminated" && x.Message.Contains("validation_failed")), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RunAsync_ShouldPropagateCorrelationIdToWorkflowEvents()
    {
        // Arrange
        var input = CreateValidInput();

        SetupSuccessfulPipeline(input);

        var engine = CreateEngine();

        // Act
        await engine.RunAsync(input, CancellationToken.None);

        // Assert
        A.CallTo(() => _eventSink.WriteAsync(A<WorkflowEvent>.That.Matches(x => x.CorrelationId == input.CorrelationId), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task RunAsync_ShouldEmitEventsInStageOrder()
    {
        // Arrange
        var input = CreateValidInput();

        SetupSuccessfulPipeline(input);

        var emitted = new List<WorkflowEvent>();

        A.CallTo(() => _eventSink.WriteAsync(A<WorkflowEvent>._, A<CancellationToken>._))
            .Invokes(call =>
            {
                var evt = call.GetArgument<WorkflowEvent>(0);

                evt.Should().NotBeNull();

                emitted.Add(evt!);
            });

        var engine = CreateEngine();

        // Act
        await engine.RunAsync(input, CancellationToken.None);

        // Assert
        emitted.Should().Contain(x => x.Stage == MarketAgentStage.Planning.ToString());
        emitted.Should().Contain(x => x.Stage == MarketAgentStage.Reasoning.ToString());
        emitted.Should().Contain(x => x.Stage == MarketAgentStage.Decision.ToString());
        emitted.FindIndex(x => x.Stage == MarketAgentStage.Planning.ToString()).Should()
            .BeLessThan(emitted.FindIndex(x => x.Stage == MarketAgentStage.Reasoning.ToString()));
    }

    private MarketAgentEngine CreateEngine()
    {
        SetupPolicies();

        var outcomeFactory = new WorkflowOutcomeFactory(NullLogger<WorkflowOutcomeFactory>.Instance);

        var stages = new IMarketAgentStage[]
        {

        new ValidationInputStage(NullLogger<ValidationInputStage>.Instance, outcomeFactory),

        new PlannerStage(_plannerAgentResolver, _store,  NullLogger<PlannerStage>.Instance, outcomeFactory),

        new PlanParsingStage(NullLogger<PlanParsingStage>.Instance, outcomeFactory),

        new ToolStage(_quoteTool,  NullLogger<ToolStage>.Instance, outcomeFactory),

        new ReasoningStage(NullLogger<ReasoningStage>.Instance,  outcomeFactory, _reasoningAgentResolver),

        new ValidationStage(_validatorAgent, NullLogger<ValidationStage>.Instance, outcomeFactory),

        new RiskStage(_riskAgent, NullLogger<RiskStage>.Instance, outcomeFactory),

        new ConfidenceStage(_confidenceScoringAgent, NullLogger<ConfidenceStage>.Instance),

        new DecisionStage(_finalDecisionAgent,  NullLogger<DecisionStage>.Instance, outcomeFactory),

        new PersistenceStage(_store,  NullLogger<PersistenceStage>.Instance, outcomeFactory)
        };

        return new MarketAgentEngine(stages, _logger, _eventSink, outcomeFactory, _policyRegistry, _orchestrator, _scheduler);
    }

    private static QuoteInsightInput CreateValidInput(
    string symbol = "AAPL",
    decimal price = 150m,
    decimal changePercent = 2m,
    long volume = 1_000_000)
    {
        return new QuoteInsightInput(symbol, price, changePercent, volume, Guid.NewGuid());
    }

    private void SetupPlanner(string plannerJson, QuoteInsightInput? input = null)
    {
        input ??= CreateValidInput();

        var cacheKey = $"{input.Symbol}:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
           .Returns(plannerJson);
    }

    private void SetupPlannerAndReasoner(QuoteInsightInput? input = null, string? plannerJson = null, string? reasonerJson = null)
    {
        input ??= CreateValidInput();

        plannerJson ??= """
        {
           "needTool": false,
           "tool": null,
           "confidence": 0.91,
           "reason": "sufficient data"
        }
        """;

        reasonerJson ??= """
        {
           "sentiment": "bullish",
           "direction": "up",
           "volatility": "medium",
           "rationale": "positive momentum"
        }
        """;

        var cacheKey = $"{input.Symbol}:{Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture)}";

        A.CallTo(() => _store.GetPlanCacheAsync(cacheKey))
            .Returns((string?)null);

        A.CallTo(() => _primaryPlannerAgent.GenerateAsync(A<QuoteInsightInput>._, A<MarketAgentWorkflowContext>._, A<MarketAgentStage>._, A<CancellationToken>._))
            .Returns(plannerJson);

        A.CallTo(() => _primaryReasoningAgent.GenerateAsync(A<QuoteInsightInput>._, A<string?>._, A<CancellationToken>._))
            .Returns(reasonerJson);
    }

    private void SetupSuccessfulValidation(QuoteInsightInput input)
    {
        A.CallTo(() => _validatorAgent.ValidateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new ValidationResult(true, "validation passed", ValidationSeverity.Low));

        A.CallTo(() => _riskAgent.EvaluateAsync(input, A<AIInsightResult>._, A<CancellationToken>._))
            .Returns(new RiskAssessmentResult(false, "risk acceptable", RiskLevel.Low));

        A.CallTo(() => _confidenceScoringAgent.ScoreAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(new ConfidenceScoreResult(0.92, ConfidenceLevel.High, "strong confidence"));

        A.CallTo(() => _finalDecisionAgent.DecideAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(new FinalDecisionResult(DecisionOutcome.Approved, "approved"));
    }

    private void SetupPolicies()
    {
        var passthroughPolicy = Policy.Handle<Exception>().RetryAsync(0);

        A.CallTo(() => _policyRegistry.GetPlannerPolicy())
            .Returns(passthroughPolicy);

        A.CallTo(() => _policyRegistry.GetReasonerPolicy())
            .Returns(passthroughPolicy);

        A.CallTo(() => _policyRegistry.GetToolingPolicy())
        .Returns(passthroughPolicy);

        A.CallTo(() => _policyRegistry.GetValidationPolicy())
            .Returns(passthroughPolicy);

        A.CallTo(() => _policyRegistry.GetDecisionPolicy())
            .Returns(passthroughPolicy);

        A.CallTo(() => _policyRegistry.GetElasticPolicy())
            .Returns(Policy.Handle<Exception>().RetryAsync(0));

        A.CallTo(() => _policyRegistry.GetDataAccessPolicy())
        .Returns(Policy.Handle<Exception>().RetryAsync(0));

        A.CallTo(() => _plannerAgentResolver.GetPrimary())
       .Returns(_primaryPlannerAgent);

        A.CallTo(() => _plannerAgentResolver.GetFallback())
            .Returns(_fallbackPlannerAgent);

        A.CallTo(() => _reasoningAgentResolver.GetPrimary())
            .Returns(_primaryReasoningAgent);

        A.CallTo(() => _reasoningAgentResolver.GetFallback())
            .Returns(_fallbackReasoningAgent);

        A.CallTo(() => _orchestrator.EvaluateExecutionAsync(A<MarketAgentWorkflowContext>._, A<IMarketAgentStage>._, A<CancellationToken>._))
            .Returns(new StageExecutionDecision(Execute: true));

        A.CallTo(() => _orchestrator.HandleFailureAsync(A<MarketAgentWorkflowContext>._, A<IMarketAgentStage>._, A<Exception>._, A<CancellationToken>._))
            .Returns(new StageFailureAction(RecoveryStrategy.Terminate, "test failure"));
    }

    private void SetupSuccessfulPipeline(QuoteInsightInput input)
    {
        SetupPlannerAndReasoner(input);

        SetupSuccessfulValidation(input);
    }

    private void SetupSuccessfulGovernancePipeline(QuoteInsightInput input)
    {
        SetupSuccessfulValidation(input);
    }
}
