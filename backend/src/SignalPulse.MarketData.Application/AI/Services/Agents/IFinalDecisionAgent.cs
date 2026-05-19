using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IFinalDecisionAgent
{
    Task<FinalDecisionResult> DecideAsync(MarketAgentWorkflowContext context, CancellationToken ct);
}