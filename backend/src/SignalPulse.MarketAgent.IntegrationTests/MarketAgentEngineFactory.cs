using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketAgent.IntegrationTests;

public static class MarketAgentEngineFactory
{
    public static EngineTestContext CreateEngine(IEnumerable<IMarketAgentStage> stages, RecoveryStrategy recoveryStrategy = RecoveryStrategy.Skip, MarketAgentStage? alternateStage = null)
    {
        var logger = A.Fake<ILogger<MarketAgentEngine>>();

        var stageMap = stages.ToDictionary(x => x.Stage);

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

        A.CallTo(() => orchestrator.ResolveStage(A<MarketAgentStage>._))
            .ReturnsLazily((MarketAgentStage s) => stageMap.GetValueOrDefault(s));

        A.CallTo(() => orchestrator.HandleFailureAsync(A<MarketAgentWorkflowContext>._, A<IMarketAgentStage>._, A<Exception>._, A<CancellationToken>._))
            .Returns(new StageFailureAction(recoveryStrategy, "test", AlternateStage: alternateStage));

        var options = Options.Create(new MarketAgentOptions
        {
            MaxParallelStages = 4
        });

        var scheduler = new MarketStageScheduler(options);

        var engine = new MarketAgentEngine(stages, logger, sink, outcomeFactory, policyRegistry, orchestrator, scheduler);

        return new EngineTestContext(engine, sink);
    }
}