using SignalPulse.MarketData.Application.AI;

namespace SignalPulse.MarketData.Application.Interfaces;

public interface IAiMarketInsightGenerator
{
    Task<AIInsightResult> GenerateAsync( string symbol, decimal price,  decimal changePercent, long volume,  CancellationToken ct = default);
}