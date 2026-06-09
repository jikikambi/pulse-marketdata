using Microsoft.Extensions.Logging;
using Polly.Timeout;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Text.Json;
using FailureClassification = SignalPulse.MarketData.Application.AI.Models.Enums.FailureClassification;
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

        var failures = ctx.State.GetFailureCount(stage.Stage);

        var escalation = EscalateRecovery(failures);

        switch (escalation)
        {
            case RecoveryStrategy.Degrade when !ctx.State.IsDegradedMode:
                return Task.FromResult(new StageFailureAction(RecoveryStrategy.Degrade, $"Entering degraded mode for {stage.Stage}"));

            case RecoveryStrategy.Terminate:
                return Task.FromResult(new StageFailureAction(RecoveryStrategy.Terminate, $"Failure threshold exceeded for {stage.Stage}"));
        }

        var classification = ClassifyFailure(ex);

        // Failure classification
        return (stage.Stage, classification) switch
        {
            (MarketAgentStage.Reasoning, FailureClassification.Timeout) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Retry, "Reasoning timed out")),
            (MarketAgentStage.Reasoning, FailureClassification.DependencyUnavailable) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Fallback, "Reasoning dependency unavailable", MarketAgentStage.Scoring)),
            (MarketAgentStage.Reasoning, FailureClassification.DataCorruption) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Terminate, "Reasoning data corruption detected")),
            (MarketAgentStage.Reasoning, FailureClassification.InfrastructureFailure) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Retry, "Reasoning infrastructure failure")),
            (MarketAgentStage.Reasoning, _) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Fallback, "Reasoning failed", MarketAgentStage.Scoring)),
            (MarketAgentStage.Validation, FailureClassification.Timeout) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Retry, "Validation timeout")),
            (MarketAgentStage.Validation, FailureClassification.DependencyUnavailable) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Degrade, "Validation dependency unavailable")),
            (MarketAgentStage.Validation, FailureClassification.DataCorruption) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Fallback, "Validation data corruption", MarketAgentStage.Decision)),
            (MarketAgentStage.Validation, FailureClassification.InfrastructureFailure) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Retry, "Validation infrastructure failure")),
            (MarketAgentStage.Validation, _) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Fallback, "Validation failed", MarketAgentStage.Decision)),
            (_, FailureClassification.Timeout) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Retry, "Operation timed out")),
            (_, FailureClassification.DependencyUnavailable) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Degrade, "Dependency unavailable")),
            (_, FailureClassification.InfrastructureFailure) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Retry, "Infrastructure failure")),
            (_, FailureClassification.DataCorruption) => Task.FromResult(new StageFailureAction(RecoveryStrategy.Terminate, "Data corruption detected")),
            _ => Task.FromResult(new StageFailureAction(RecoveryStrategy.Terminate, ex.Message))
        };
    }

    public IMarketAgentStage? ResolveStage(MarketAgentStage stage) => _stageMap.GetValueOrDefault(stage);

    private static RecoveryStrategy EscalateRecovery(int failures) => failures switch
    {
        <= 2 => RecoveryStrategy.Fallback,
        3 => RecoveryStrategy.Degrade,
        >= 4 => RecoveryStrategy.Terminate
    };

    private static FailureClassification ClassifyFailure(Exception ex) => ex switch
    {
        TimeoutRejectedException => FailureClassification.Timeout,
        TaskCanceledException => FailureClassification.Timeout,
        HttpRequestException => FailureClassification.DependencyUnavailable,
        JsonException => FailureClassification.DataCorruption,

        _ when IsInfrastructureException(ex) => FailureClassification.InfrastructureFailure,

        _ => FailureClassification.Unknown
    };

    private static bool IsInfrastructureException(Exception ex)
    {
        var name = ex.GetType().Name;

        return name.Contains("Sql")
            || name.Contains("Npgsql")
            || name.Contains("Mongo")
            || name.Contains("Redis");
    }
}