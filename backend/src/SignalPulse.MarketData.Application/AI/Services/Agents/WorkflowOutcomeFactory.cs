using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class WorkflowOutcomeFactory(ILogger<WorkflowOutcomeFactory> logger) : IWorkflowOutcomeFactory
{
    public AIInsightResult Safe(MarketAgentWorkflowContext ctx, string reason)
    {
        ctx.AddStep(AgentConstants.StepSafe, reason, "");

        logger.LogWarning("Safe fallback triggered for {Symbol}: {Reason}", ctx.Input.Symbol, reason);

        return new AIInsightResult(SentimentType.Neutral, DirectionType.Sideways, VolatilityType.Low, $"safe_fallback: {reason}");
    }

    public AIInsightResult Unsafe(MarketAgentWorkflowContext ctx, string reason)
    {
        ctx.AddStep(AgentConstants.StepUnsafe, reason, "");

        logger.LogError("Unsafe exit for {Symbol}: {Reason}", ctx.Input.Symbol, reason);

        return new AIInsightResult(SentimentType.Neutral, DirectionType.Sideways, VolatilityType.High, $"rejected_input: {reason}");
    }
}