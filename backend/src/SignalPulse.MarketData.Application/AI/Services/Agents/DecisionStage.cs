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

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var decision = await finalDecisionAgent.DecideAsync(ctx, ct);

        ctx.FinalDecision = decision;

        ctx.State.FinalDecision = decision;

        if (decision.Outcome != DecisionOutcome.Approved)
        {
            ctx.Terminate(outcomeFactory.Safe(ctx, $"decision_{decision.Outcome.ToString().ToLowerInvariant()}"));
        }

        Logger.LogInformation("FinalDecisionAgent decided {Symbol}. Outcome: {Outcome}, Reason: {Reason}", ctx.Input.Symbol, decision.Outcome, decision.Reason);
    }
}