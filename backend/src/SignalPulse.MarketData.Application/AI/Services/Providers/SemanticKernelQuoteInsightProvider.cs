using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Prompts;
using SignalPulse.MarketData.Application.Interfaces;
namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public sealed class SemanticKernelQuoteInsightProvider(QuoteInsightPrompt prompt) : IAiInsightProvider<QuoteInsightInput>
{
    public Task<AIInsightResult> GenerateAsync(QuoteInsightInput input, CancellationToken ct = default) =>
        prompt.AnalyzeAsync(input.Symbol, input.Price, input.ChangePercent, input.Volume, ct);
}