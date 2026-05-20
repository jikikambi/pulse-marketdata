using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.Interfaces;
namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public sealed class SemanticKernelForexInsightProvider(ForexInsightService prompt) : IAiInsightProvider<ForexInsightInput>
{
    public Task<AIInsightResult> GenerateAsync(ForexInsightInput input, CancellationToken ct = default) =>
        prompt.AnalyzeAsync(input.FromSymbol, input.ToSymbol, input.Open, input.High, input.Low, input.Close, ct);
}