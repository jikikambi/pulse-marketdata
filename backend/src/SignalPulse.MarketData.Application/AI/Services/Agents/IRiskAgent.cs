using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IRiskAgent
{
    Task<RiskAssessmentResult> EvaluateAsync(QuoteInsightInput input, AIInsightResult insight, CancellationToken ct);
}