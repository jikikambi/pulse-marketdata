using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IReasoningAgent
{
    string Name { get; }
    Task<string?> GenerateAsync(QuoteInsightInput input, string? toolContext, CancellationToken ct);
}