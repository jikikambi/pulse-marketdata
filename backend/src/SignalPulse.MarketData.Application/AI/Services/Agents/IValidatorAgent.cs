using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IValidatorAgent
{
    Task<ValidationResult> ValidateAsync(QuoteInsightInput input, AIInsightResult insight, CancellationToken ct);
}
