using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;
namespace SignalPulse.MarketData.Application.Services;

public sealed class SemanticKernelQuoteInsightProvider(QuoteInsightPrompt prompt) : IAiInsightProvider<QuoteInsightInput>
{
    public Task<AIInsightResult> GenerateAsync(QuoteInsightInput input, CancellationToken ct = default)
    {
        return prompt.AnalyzeAsync(input.Symbol, input.Price, input.ChangePercent, input.Volume, ct);
    }
}