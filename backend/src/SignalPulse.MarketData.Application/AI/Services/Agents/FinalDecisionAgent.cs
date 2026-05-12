using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class FinalDecisionAgent : IFinalDecisionAgent
{
    public Task<FinalDecisionResult> DecideAsync(MarketAgentWorkflowContext context, CancellationToken ct)
    {
        // Validation failed

        if (context.Validation?.IsValid == false)
        {
            return Task.FromResult(new FinalDecisionResult(DecisionOutcome.Rejected, "Validation failed"));
        }

        // Risk rejection

        if (context.Risk?.IsRisky == true)
        {
            return Task.FromResult(new FinalDecisionResult(DecisionOutcome.Rejected, "Risk threshold exceeded"));
        }

        // Low confidence

        if (context.Confidence?.Level == ConfidenceLevel.Low)
        {
            return Task.FromResult(new FinalDecisionResult(DecisionOutcome.Held, "Low confidence insight"));
        }

        // High volatility escalation

        if (context.Insight?.Volatility == VolatilityType.High)
        {
            return Task.FromResult(new FinalDecisionResult(DecisionOutcome.Escalated, "High volatility requires review"));
        }

        return Task.FromResult(new FinalDecisionResult(DecisionOutcome.Approved, "Insight approved"));
    }
}