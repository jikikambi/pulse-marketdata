using Microsoft.Extensions.Logging;
using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class MarketAgentEngine(IEnumerable<IMarketAgentStage> stages,
    ILogger<MarketAgentEngine> logger,
    IWorkflowEventSink eventSink,
    IWorkflowOutcomeFactory outcomeFactory,
    IAiPolicyRegistry policyRegistry,
    IMarketStageOrchestrator orchestrator,
    IMarketStageScheduler scheduler)
{
    private static readonly ActivitySource ActivitySource = new("SignalPulse.MarketAgent");
    private static readonly Meter Meter = new("SignalPulse.MarketAgent");
    private static readonly Histogram<double> WorkflowDuration = Meter.CreateHistogram<double>("marketagent.workflow.duration");
    private static readonly Counter<long> WorkflowCompleted = Meter.CreateCounter<long>("marketagent.workflow.completed");
    private static readonly Counter<long> WorkflowFailed = Meter.CreateCounter<long>("marketagent.workflow.failed");
    private readonly IAiPolicyRegistry _policyRegistry = policyRegistry;

    private readonly IReadOnlyList<IMarketAgentStage> _stages = [.. stages.OrderBy(x => x.Stage)];

    public async Task<AIInsightResult> RunAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var workflowActivity = ActivitySource.StartActivity("MarketAgent.Workflow", ActivityKind.Internal);

        workflowActivity?.SetTag("workflow.symbol", input.Symbol);
        workflowActivity?.SetTag("workflow.correlation_id", input.CorrelationId);
        workflowActivity?.SetTag("workflow.started_at", DateTimeOffset.UtcNow);

        logger.LogInformation("Starting market agent workflow for {Symbol}", input.Symbol);

        var ctx = new MarketAgentWorkflowContext
        {
            Input = input,
            EventSink = eventSink,
            State = new MarketAgentState
            {
                Symbol = input.Symbol,
                CorrelationId = input.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        using var workflowCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        ctx.WorkflowCancellationSource = workflowCts;

        try
        {
            scheduler.Initialize(_stages);

            while (scheduler.HasRemainingStages && !ctx.IsTerminated)
            {
                var batch = scheduler.GetExecutableStages();

                if (batch.Count == 0)
                    break;

                await ExecuteBatchAsync(batch, ctx, workflowActivity, sw, workflowCts.Token);
            }

            sw.Stop();

            workflowActivity?.SetTag("workflow.duration_ms", sw.ElapsedMilliseconds);
            workflowActivity?.SetTag("workflow.stage_count", ctx.StageResults.Count);
            workflowActivity?.SetTag("workflow.degraded_mode", ctx.State.IsDegradedMode);
            workflowActivity?.SetTag("workflow.retry_count", ctx.State.RetryCount);

            workflowActivity?.SetStatus(ActivityStatusCode.Ok);

            WorkflowCompleted.Add(1);

            WorkflowDuration.Record(sw.Elapsed.TotalMilliseconds);

            logger.LogInformation("Workflow completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);

            return ctx.FinalResult ?? ctx.Insight ?? throw new InvalidOperationException($"Workflow completed without result for {input.Symbol}");
        }
        catch (Exception ex)
        {
            sw.Stop();

            workflowActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            workflowActivity?.AddException(ex);

            WorkflowFailed.Add(1, new TagList
            {
                { "reason", "unhandled_exception" }
            });

            WorkflowDuration.Record(sw.Elapsed.TotalMilliseconds);

            logger.LogCritical(ex, "Unhandled workflow failure for {Symbol} after {ElapsedMs}ms", input.Symbol, sw.ElapsedMilliseconds);

            return outcomeFactory.Safe(ctx, "workflow_unhandled_exception");
        }
    }

    private async Task ExecuteBatchAsync(IReadOnlyCollection<IMarketAgentStage> stages, MarketAgentWorkflowContext ctx, Activity? workflowActivity, Stopwatch sw, CancellationToken ct)
    {
        var tasks = stages.Select(stage => ExecuteStageAsync(stage, ctx, workflowActivity, sw, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ExecuteStageAsync(IMarketAgentStage stage, MarketAgentWorkflowContext ctx, Activity? workflowActivity, Stopwatch sw, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity($"Stage.{stage.Stage}");
        activity?.SetTag("stage.name", stage.Stage.ToString());
        activity?.SetTag("workflow.symbol", ctx.Input.Symbol);
        activity?.SetTag("workflow.correlation_id", ctx.Input.CorrelationId);

        if (ct.IsCancellationRequested || ctx.IsTerminated)
        {
            scheduler.MarkSkipped(stage.Stage);
            activity?.AddEvent(new ActivityEvent("stage_skipped"));
            workflowActivity?.AddEvent(new ActivityEvent($"stage_skipped:{stage.Stage}"));
            return;
        }

        var decision = await orchestrator.EvaluateExecutionAsync(ctx, stage, ct);

        activity?.AddEvent(new ActivityEvent("execution_decision"));
        activity?.SetTag("stage.execute", decision.Execute);
        activity?.SetTag("stage.reason", decision.Reason);

        if (!decision.Execute)
        {
            logger.LogInformation("Skipping stage {Stage}. Reason: {Reason}", stage.Stage, decision.Reason);

            scheduler.MarkSkipped(stage.Stage);

            activity?.AddEvent(new ActivityEvent("stage_skipped"));
            workflowActivity?.AddEvent(new ActivityEvent($"stage_skipped:{stage.Stage}"));

            return;
        }

        logger.LogDebug("Executing stage {Stage} for {Symbol}", stage.Stage, ctx.Input.Symbol);

        var policy = ResolvePolicy(stage.Stage);

        var pollyContext = new Context(stage.Stage.ToString());

        try
        {
            await policy.ExecuteAsync(async (context, token) =>
            {
                context["emitter"] = ctx;

                await stage.ExecuteAsync(ctx, token);

                scheduler.MarkCompleted(stage.Stage);
                activity?.SetStatus(ActivityStatusCode.Ok);

            }, pollyContext, ct);

            scheduler.MarkCompleted(stage.Stage);

            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.AddEvent(new ActivityEvent("stage_completed"));
        }
        catch (OperationCanceledException)
        {
            scheduler.MarkSkipped(stage.Stage);

            activity?.AddEvent(new ActivityEvent("stage_cancelled"));
            workflowActivity?.AddEvent(new ActivityEvent($"stage_cancelled:{stage.Stage}"));

            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            await HandleStageFailureAsync(stage, ctx, workflowActivity, sw, ex, ct);
        }
    }

    private async Task HandleStageFailureAsync(IMarketAgentStage stage, MarketAgentWorkflowContext ctx, Activity? workflowActivity, Stopwatch sw, Exception ex, CancellationToken ct)
    {
        scheduler.MarkFailed(stage.Stage);

        var failure = await orchestrator.HandleFailureAsync(ctx, stage, ex, ct);

        if (failure.UseFallback && failure.FallbackStage is not null)
        {
            var fallback = orchestrator.ResolveStage(failure.FallbackStage.Value);

            if (fallback is not null)
            {
                logger.LogWarning("Executing fallback stage {FallbackStage}", failure.FallbackStage);

                workflowActivity?.AddEvent(new ActivityEvent($"fallback:{failure.FallbackStage}"));

                try
                {
                    await fallback.ExecuteAsync(ctx, ct);
                    scheduler.MarkCompleted(failure.FallbackStage.Value);
                    return;
                }
                catch (Exception fbEx)
                {
                    scheduler.MarkFailed(failure.FallbackStage.Value);
                    throw new InvalidOperationException("Fallback execution failed", fbEx);
                }

            }
        }

        if (failure.TerminateWorkflow)
        {
            sw.Stop();

            workflowActivity?.SetStatus(ActivityStatusCode.Error, failure.Reason);

            WorkflowFailed.Add(1, new TagList
            {
                { "reason", "stage_failure" }
            });

            WorkflowDuration.Record(sw.Elapsed.TotalMilliseconds);

            logger.LogCritical(ex, "Workflow terminating after stage failure");

            ctx.Terminate(outcomeFactory.Safe(ctx, failure.Reason ?? "workflow_failure"));
            ctx.WorkflowCancellationSource?.Cancel();
            return;
        }
        throw ex;
    }

    private IAsyncPolicy ResolvePolicy(MarketAgentStage stage)
    {
        return stage switch
        {
            MarketAgentStage.Planning => _policyRegistry.GetPlannerPolicy(),

            MarketAgentStage.Reasoning => _policyRegistry.GetReasonerPolicy(),

            MarketAgentStage.Tooling => _policyRegistry.GetToolingPolicy(),

            MarketAgentStage.Validation => _policyRegistry.GetValidationPolicy(),

            MarketAgentStage.Decision => _policyRegistry.GetDecisionPolicy(),

            MarketAgentStage.Persistence => _policyRegistry.GetDataAccessPolicy(),

            _ => Policy.NoOpAsync()
        };
    }
}