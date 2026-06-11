using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;

namespace SignalPulse.MarketAgent.IntegrationTests;

public sealed class ExplodingStage : IMarketAgentStage
{
    public MarketAgentStage Stage => MarketAgentStage.Reasoning;
    public IReadOnlyCollection<MarketAgentStage> DependsOn => [];
    public Task ExecuteAsync(MarketAgentWorkflowContext ctx, CancellationToken ct) => throw new InvalidOperationException("boom");
}
