using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;

namespace SignalPulse.MarketAgent.IntegrationTests;

public sealed class SlowStage : IMarketAgentStage
{
    public MarketAgentStage Stage => MarketAgentStage.Planning;
    public IReadOnlyCollection<MarketAgentStage> DependsOn => [];
    public async Task ExecuteAsync(MarketAgentWorkflowContext ctx, CancellationToken ct) => await Task.Delay(50, ct);
}