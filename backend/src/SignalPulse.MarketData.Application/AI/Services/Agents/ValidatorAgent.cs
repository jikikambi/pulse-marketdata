using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ValidatorAgent : IValidatorAgent
{
    public Task<ValidationResult> ValidateAsync(QuoteInsightInput input, AIInsightResult insight, CancellationToken ct)
    {
        // Structural validation

        if (insight is null)
        {
            return Task.FromResult(new ValidationResult(false, "Insight result is null", ValidationSeverity.Critical));
        }

        if (string.IsNullOrWhiteSpace(insight.Rationale))
        {
            return Task.FromResult(new ValidationResult(false, "Missing rationale", ValidationSeverity.High));
        }

        if (insight.Sentiment is not (SentimentType.Bullish or SentimentType.Bearish or SentimentType.Neutral))
        {
            return Task.FromResult(new ValidationResult(false, "Invalid sentiment", ValidationSeverity.High));
        }

        // Semantic validation

        if (insight.Sentiment == SentimentType.Bullish && insight.Direction == DirectionType.Down)
        {
            return Task.FromResult(new ValidationResult(false, "Bullish sentiment conflicts with downward direction", ValidationSeverity.Critical));
        }

        return Task.FromResult(new ValidationResult(true, "Validation passed", ValidationSeverity.Low));
    }
}