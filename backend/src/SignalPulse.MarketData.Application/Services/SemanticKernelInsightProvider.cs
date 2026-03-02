using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.Services;

public partial class SemanticKernelInsightProvider(QuoteInsightPrompt prompt) : IAiInsightProvider
{
    public async Task<AIInsightResult> GenerateAsync(string symbol,
        decimal price,
        decimal changePercent, 
        long volume, 
        CancellationToken ct = default)
    {   
        return await prompt.AnalyzeAsync(symbol, price, changePercent, volume, ct);
    }
}