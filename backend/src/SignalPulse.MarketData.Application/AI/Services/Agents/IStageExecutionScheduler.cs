using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IStageExecutionScheduler
{
    Task ExecuteAsync(IReadOnlyCollection<IMarketAgentStage> stages, MarketAgentWorkflowContext ctx, CancellationToken ct);
}