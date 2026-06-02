using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class DecisionStage(IFinalDecisionAgent finalDecisionAgent,
    ILogger<DecisionStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<DecisionStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.Decision;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [MarketAgentStage.Scoring];

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        await ctx.EmitAsync(Stage.ToString(), "decision_started", "Final decision stage started", null, ct);

        var decision = await finalDecisionAgent.DecideAsync(ctx, ct);

        ctx.FinalDecision = decision;

        ctx.State.FinalDecision = decision;

        await ctx.EmitAsync(Stage.ToString(),
            decision.Outcome == DecisionOutcome.Approved ? "decision_approved" : "decision_rejected", decision.Reason, new
            {
                Outcome = decision.Outcome.ToString()
            }, ct);

        if (decision.Outcome != DecisionOutcome.Approved)
        {
            ctx.Terminate(outcomeFactory.Safe(ctx, $"decision_{decision.Outcome.ToString().ToLowerInvariant()}"));
        }

        Logger.LogInformation("FinalDecisionAgent decided {Symbol}. Outcome: {Outcome}, Reason: {Reason}", ctx.Input.Symbol, decision.Outcome, decision.Reason);
    }
}