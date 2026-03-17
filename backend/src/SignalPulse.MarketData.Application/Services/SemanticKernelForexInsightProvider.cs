using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;
namespace SignalPulse.MarketData.Application.Services;

public sealed class SemanticKernelForexInsightProvider(ForexInsightPrompt prompt) : IAiInsightProvider<ForexInsightInput>
{
    public Task<AIInsightResult> GenerateAsync(ForexInsightInput input, CancellationToken ct = default)
    {
        return prompt.AnalyzeAsync(input.FromSymbol, input.ToSymbol, input.Open, input.High, input.Low, input.Close, ct);
    }
}