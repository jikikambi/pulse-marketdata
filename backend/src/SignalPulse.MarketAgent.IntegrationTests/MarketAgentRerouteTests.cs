using FakeItEasy;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Infrastructure.Elastic;

namespace SignalPulse.MarketAgent.IntegrationTests;

public class MarketAgentRerouteTests
{
    [Fact]
    public async Task Reroute_emits_workflow_rerouted_event()
    {
        var riskStage = A.Fake<IMarketAgentStage>();

        A.CallTo(() => riskStage.Stage).Returns(MarketAgentStage.RiskEvaluation);

        A.CallTo(() => riskStage.DependsOn).Returns([MarketAgentStage.Reasoning]);

        A.CallTo(() => riskStage.ExecuteAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        var (engine, sink) = MarketAgentEngineFactory.CreateEngine([new ExplodingStage(), riskStage], RecoveryStrategy.Reroute, MarketAgentStage.RiskEvaluation);

        await engine.RunAsync(CreateInput(), CancellationToken.None);

        A.CallTo(() => sink.WriteAsync(A<WorkflowEvent>.That.Matches(x => x.EventType == "workflow_rerouted"), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Reroute_executes_alternate_stage()
    {
        var riskStage = A.Fake<IMarketAgentStage>();

        A.CallTo(() => riskStage.Stage).Returns(MarketAgentStage.RiskEvaluation);

        A.CallTo(() => riskStage.DependsOn).Returns([MarketAgentStage.Reasoning]);

        A.CallTo(() => riskStage.ExecuteAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        var ctx = MarketAgentEngineFactory.CreateEngine([new ExplodingStage(), riskStage], RecoveryStrategy.Reroute, MarketAgentStage.RiskEvaluation);

        await ctx.Engine.RunAsync(CreateInput(), CancellationToken.None);

        A.CallTo(() => riskStage.ExecuteAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private static QuoteInsightInput CreateInput() => new("MSFT", 100m, 2m, 1000, Guid.NewGuid());
}