using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IMarketStageOrchestrator
{
    Task<StageExecutionDecision> EvaluateExecutionAsync( MarketAgentWorkflowContext ctx, IMarketAgentStage stage,  CancellationToken ct);
    Task<StageFailureAction> HandleFailureAsync( MarketAgentWorkflowContext ctx, IMarketAgentStage stage, Exception ex, CancellationToken ct);
    IMarketAgentStage? ResolveStage(MarketAgentStage stage);
}