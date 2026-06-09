using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Application.UnitTests.AI;

public class MarketAgentEngineObservabilityTests
{
    [Fact]
    public async Task WorkflowStarted_metric_increments()
    {
        using var metrics = new TestMetricCollector();

        var engine = CreateEngine([new SlowStage()]);

        await engine.RunAsync(CreateInput(), CancellationToken.None);

        metrics.Counters["marketagent.workflow.started"].Should().Be(1);
    }

    [Fact]
    public async Task WorkflowCompleted_metric_increments()
    {
        using var metrics = new TestMetricCollector();

        var engine = CreateEngine([new SlowStage()]);

        await engine.RunAsync(CreateInput(), CancellationToken.None);

        metrics.Counters.Should().ContainKey("marketagent.workflow.completed");
        metrics.Counters["marketagent.workflow.completed"].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WorkflowFailed_metric_increments()
    {
        using var metrics = new TestMetricCollector();

        var engine = CreateEngine([new ExplodingStage()], RecoveryStrategy.Terminate);

        await engine.RunAsync(CreateInput(), CancellationToken.None);

        metrics.Counters["marketagent.workflow.failed"].Should().Be(1);
    }

    [Fact]
    public async Task RecoveryApplied_metric_increments()
    {
        using var metrics = new TestMetricCollector();

        var engine = CreateEngine([new ExplodingStage()], RecoveryStrategy.Skip);

        await engine.RunAsync(CreateInput(), CancellationToken.None);

        metrics.Counters["marketagent.workflow.recoveries"].Should().Be(1);
    }

    [Fact]
    public async Task StageDuration_records_measurement()
    {
        using var metrics = new TestMetricCollector();

        var engine = CreateEngine([new SlowStage()]);

        await engine.RunAsync(CreateInput(), CancellationToken.None);

        metrics.Histograms.Should().ContainKey("marketagent.stage.duration");

        metrics.Histograms["marketagent.stage.duration"].Should().NotBeEmpty();

        metrics.Histograms["marketagent.stage.duration"].First().Should().BeGreaterThan(0);
    }

    private static QuoteInsightInput CreateInput() => new("AAPL", 210.15m, 1.25m, 1_000_000, Guid.NewGuid());

    private static MarketAgentEngine CreateEngine(IEnumerable<IMarketAgentStage> stages, RecoveryStrategy recoveryStrategy = RecoveryStrategy.Skip)
    {
        var logger = A.Fake<ILogger<MarketAgentEngine>>();

        var sink = A.Fake<IWorkflowEventSink>();

        var outcomeFactory = A.Fake<IWorkflowOutcomeFactory>();

        var policyRegistry = A.Fake<IAiPolicyRegistry>();

        A.CallTo(() => policyRegistry.GetPlannerPolicy())
            .Returns(Policy.NoOpAsync());

        A.CallTo(() => policyRegistry.GetReasonerPolicy())
            .Returns(Policy.NoOpAsync());

        A.CallTo(() => policyRegistry.GetToolingPolicy())
            .Returns(Policy.NoOpAsync());

        A.CallTo(() => policyRegistry.GetValidationPolicy())
            .Returns(Policy.NoOpAsync());

        A.CallTo(() => policyRegistry.GetDecisionPolicy())
            .Returns(Policy.NoOpAsync());

        A.CallTo(() => policyRegistry.GetDataAccessPolicy())
            .Returns(Policy.NoOpAsync());

        var orchestrator = A.Fake<IMarketStageOrchestrator>();

        A.CallTo(() => orchestrator.EvaluateExecutionAsync(A<MarketAgentWorkflowContext>._, A<IMarketAgentStage>._, A<CancellationToken>._))
            .Returns(new StageExecutionDecision(Execute: true, Reason: null));

        A.CallTo(() => orchestrator.HandleFailureAsync(A<MarketAgentWorkflowContext>._, A<IMarketAgentStage>._, A<Exception>._, A<CancellationToken>._))
            .Returns(new StageFailureAction(recoveryStrategy, "test"));

        var options = Options.Create(new MarketAgentOptions
        {
            MaxParallelStages = 4
        });

        var scheduler = new MarketStageScheduler(options);

        return new MarketAgentEngine(stages, logger, sink, outcomeFactory, policyRegistry, orchestrator, scheduler);
    }
}