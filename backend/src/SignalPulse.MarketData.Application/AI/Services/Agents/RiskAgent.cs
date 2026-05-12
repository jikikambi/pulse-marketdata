using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class RiskAgent : IRiskAgent
{
    public Task<RiskAssessmentResult> EvaluateAsync(QuoteInsightInput input, AIInsightResult insight, CancellationToken ct)
    {
        // High volatility + extreme movement
        if (insight.Volatility == VolatilityType.High && Math.Abs(input.ChangePercent) >= 10)
        {
            return Task.FromResult(new RiskAssessmentResult(true, "High volatility with extreme price movement", RiskLevel.High));
        }

        // Bearish + large negative movement
        if (insight.Sentiment == SentimentType.Bearish && input.ChangePercent <= -8)
        {
            return Task.FromResult(new RiskAssessmentResult(true, "Strong bearish movement detected", RiskLevel.High));
        }

        return Task.FromResult(new RiskAssessmentResult(false, "Risk acceptable", RiskLevel.Low));
    }
}