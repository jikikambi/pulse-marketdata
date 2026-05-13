using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IWorkflowOutcomeFactory
{
    AIInsightResult Safe(MarketAgentWorkflowContext ctx, string reason);
    AIInsightResult Unsafe(MarketAgentWorkflowContext ctx, string reason);
}
