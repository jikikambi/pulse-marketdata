using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class MarketStageOrchestrator(IEnumerable<IMarketAgentStage> stages,
    ILogger<MarketStageOrchestrator> logger)
    : IMarketStageOrchestrator
{
    private readonly Dictionary<MarketAgentStage, IMarketAgentStage> _stageMap = stages.ToDictionary(x => x.Stage);

    public Task<StageExecutionDecision> EvaluateExecutionAsync(MarketAgentWorkflowContext ctx, IMarketAgentStage stage, CancellationToken ct)
    {
        /*
         * CONDITIONAL EXECUTION
         */

        if (stage.Stage == MarketAgentStage.Tooling && ctx.Plan?.NeedTool != true)
        {
            return Task.FromResult(new StageExecutionDecision(
                Execute: false,
                Skip: true,
                Reason: "Tooling not required"));
        }

        /*
         * DEGRADED EXECUTION MODE
         */

        if (ctx.State.RetryCount >= 3 && stage.Stage == MarketAgentStage.Reasoning)
        {
            ctx.State.IsDegradedMode = true;

            return Task.FromResult(new StageExecutionDecision(
                Execute: true,
                UseDegradedMode: true,
                Reason: "Workflow degraded mode enabled"));
        }

        return Task.FromResult(new StageExecutionDecision());
    }

    public Task<StageFailureAction> HandleFailureAsync(MarketAgentWorkflowContext ctx, IMarketAgentStage stage, Exception ex, CancellationToken ct)
    {
        logger.LogWarning(ex, "Handling workflow stage failure for {Stage}", stage.Stage);

        /*
         * FALLBACK STAGES
         */

        if (stage.Stage == MarketAgentStage.Reasoning)
        {
            return Task.FromResult(new StageFailureAction(
                ContinueWorkflow: true,
                RetryStage: false,
                UseFallback: true,
                TerminateWorkflow: false,
                FallbackStage: MarketAgentStage.Scoring,
                Reason: "Reasoning failed, fallback to scoring"));
        }

        /*
         * RECOVERY STRATEGY
         */

        if (stage.Stage == MarketAgentStage.Validation)
        {
            return Task.FromResult(new StageFailureAction(
                ContinueWorkflow: true,
                RetryStage: false,
                UseFallback: true,
                TerminateWorkflow: false,
                FallbackStage: MarketAgentStage.Decision,
                Reason: "Validation failed, continuing with safe decision"));
        }

        /*
         * TERMINATION
         */

        return Task.FromResult(new StageFailureAction(
            ContinueWorkflow: false,
            RetryStage: false,
            UseFallback: false,
            TerminateWorkflow: true,
            Reason: ex.Message));
    }

    public IMarketAgentStage? ResolveStage(MarketAgentStage stage)
    {
        return _stageMap.GetValueOrDefault(stage);
    }
}