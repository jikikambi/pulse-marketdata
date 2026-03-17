using SignalPulse.MarketData.Application.AI;

namespace SignalPulse.MarketData.Application.Interfaces;

public interface IAiInsightProvider<TInput>
{
    Task<AIInsightResult> GenerateAsync(TInput input, CancellationToken ct = default);
}