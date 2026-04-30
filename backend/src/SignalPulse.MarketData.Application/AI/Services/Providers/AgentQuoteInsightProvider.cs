using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public sealed class AgentQuoteInsightProvider(MarketAgentEngine agent) : IAiInsightProvider<QuoteInsightInput>
{
    public Task<AIInsightResult> GenerateAsync(QuoteInsightInput input, CancellationToken ct = default) =>
        agent.RunAsync(input, ct);
}