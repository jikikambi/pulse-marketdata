using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IConfidenceScoringAgent
{
    Task<ConfidenceScoreResult> ScoreAsync(MarketAgentWorkflowContext context, CancellationToken ct);
}
