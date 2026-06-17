using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IMarketAgentStage
{
    MarketAgentStage Stage { get; }
    IReadOnlyCollection<MarketAgentStage> DependsOn { get; }
    Task ExecuteAsync(MarketAgentWorkflowContext ctx, CancellationToken ct);
}