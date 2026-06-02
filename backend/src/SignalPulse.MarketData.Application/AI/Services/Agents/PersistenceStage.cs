using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Memory;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class PersistenceStage(IAgentStateStore store,
    ILogger<PersistenceStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<PersistenceStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.Persistence;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [MarketAgentStage.Decision];

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        try
        {
            // Completion metadata

            ctx.State.Completed = true;

            ctx.State.StageResults = ctx.StageResults;

            ctx.State.UpdatedAt = DateTimeOffset.UtcNow;

            // Final governance enforcement

            if (!ctx.IsTerminated &&
                ctx.FinalDecision is not null &&
                ctx.FinalDecision.Outcome != DecisionOutcome.Approved)
            {
                Logger.LogWarning("Final decision rejected workflow for {Symbol}. Outcome: {Outcome}", ctx.Input.Symbol, ctx.FinalDecision.Outcome);

                ctx.Terminate(outcomeFactory.Safe(ctx, $"decision_{ctx.FinalDecision.Outcome.ToString().ToLowerInvariant()}"));
            }

            // Persist state (with a workflow execution identity key)

            var key = $"agent:{ctx.Input.Symbol}:{ctx.Input.CorrelationId}";

            await store.SetAsync(key, ctx.State);

            Logger.LogInformation("Workflow persisted for {Symbol}. Completed: {Completed}, Terminated: {Terminated}, ToolUsed: {ToolUsed}", ctx.Input.Symbol, ctx.State.Completed, ctx.IsTerminated, ctx.State.ToolUsed);

            // Final result resolution

            if (ctx.FinalResult is null && ctx.Insight is null)
            {
                Logger.LogError("Workflow completed without final result or insight for {Symbol}", ctx.Input.Symbol);

                ctx.Terminate(outcomeFactory.Safe(ctx, "workflow_completed_without_result"));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Persistence stage failed for {Symbol}", ctx.Input.Symbol);

            ctx.Terminate(outcomeFactory.Safe(ctx, "persistence_failed"));
        }
    }
}