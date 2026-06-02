using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ConfidenceStage(IConfidenceScoringAgent confidenceScoringAgent,
    ILogger<ConfidenceStage> logger)
    : MarketAgentStageBase<ConfidenceStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.Scoring;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [MarketAgentStage.Validation, MarketAgentStage.RiskEvaluation];

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        await ctx.EmitAsync(Stage.ToString(), "confidence_started", "Confidence scoring started", null, ct);

        var confidence = await confidenceScoringAgent.ScoreAsync(ctx, ct);

        ctx.Confidence = confidence;

        ctx.State.Confidence = confidence;

        Logger.LogInformation("ConfidenceScoringAgent evaluated {Symbol}. Score: {Score}, Level: {Level}", ctx.Input.Symbol, confidence.Score, confidence.Level);

        await ctx.EmitAsync(Stage.ToString(),
            "confidence_scored", confidence.Reason, new
            {
                confidence.Score,
                Level = confidence.Level.ToString()
            }, ct);

        Logger.LogInformation("Confidence computed for {Symbol}: {Score} ({Level})", ctx.Input.Symbol, confidence.Score, confidence.Level);
    }
}