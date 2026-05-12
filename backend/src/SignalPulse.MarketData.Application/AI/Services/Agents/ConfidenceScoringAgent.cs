using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ConfidenceScoringAgent : IConfidenceScoringAgent
{
    public Task<ConfidenceScoreResult> ScoreAsync(MarketAgentWorkflowContext context, CancellationToken ct)
    {
        double score = 0;

        // Planner confidence

        score += (context.Plan?.Confidence ?? 0) * 40;

        // Validation bonus

        if (context.Validation?.IsValid == true)
        {
            score += 20;
        }

        // Tool enrichment bonus

        if (!string.IsNullOrWhiteSpace(context.ToolContextJson))
        {
            score += 15;
        }

        // Risk penalties

        if (context.Risk?.Level == RiskLevel.High)
        {
            score -= 30;
        }

        // Volatility penalties

        if (context.Insight?.Volatility == VolatilityType.High)
        {
            score -= 15;
        }

        // Clamp

        score = Math.Clamp(score, 0, 100);

        // Determine confidence level

        var level = score switch
        {
            >= 85 => ConfidenceLevel.VeryHigh,
            >= 70 => ConfidenceLevel.High,
            >= 50 => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };

        return Task.FromResult(new ConfidenceScoreResult(score, level, $"Confidence score computed: {score:F1}"));
    }
}