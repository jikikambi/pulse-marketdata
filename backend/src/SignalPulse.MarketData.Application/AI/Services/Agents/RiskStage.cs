using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class RiskStage(IRiskAgent riskAgent,
    ILogger<RiskStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<RiskStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.RiskEvaluation;

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var risk = await riskAgent.EvaluateAsync(ctx.Input, ctx.Insight!, ct);

        ctx.Risk = risk;

        ctx.AddStep(AgentConstants.StepRisker, 
            JsonSerializer.Serialize(new RiskAuditInput(ctx.Input.Symbol, ctx.Input.ChangePercent, ctx.Insight!.Volatility, ctx.Insight.Sentiment)), 
            JsonSerializer.Serialize(risk));

        Logger.LogInformation("RiskAgent evaluated {Symbol}. Risky: {IsRisky}, Level: {Level}, Reason: {Reason}", ctx.Input.Symbol, risk.IsRisky, risk.Level, risk.Reason);

        if (risk.IsRisky)
        {
            Logger.LogWarning("RiskAgent blocked execution for {Symbol}: {Reason}", ctx.Input.Symbol, risk.Reason);

            ctx.Terminate(outcomeFactory.Safe(ctx, "risk_threshold_exceeded"));
        }
    }
}