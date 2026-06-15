using Microsoft.Extensions.Logging;
using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;
using System.Diagnostics;

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
    private readonly IAiPolicyRegistry _policyRegistry = policyRegistry;

    private readonly IReadOnlyList<IMarketAgentStage> _stages = [.. stages.OrderBy(x => x.Stage)];

    public async Task<AIInsightResult> RunAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var workflowActivity = ActivitySource.StartActivity("MarketAgent.Workflow", ActivityKind.Internal);

        workflowActivity?.SetTag("workflow.symbol", input.Symbol);
        workflowActivity?.SetTag("workflow.correlation_id", input.CorrelationId);
        workflowActivity?.SetTag("workflow.started_at", DateTimeOffset.UtcNow);

        ObservabilityMetrics.WorkflowStarted.Add(1);
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

            ObservabilityMetrics.WorkflowCompleted.Add(1);

            ObservabilityMetrics.WorkflowDuration.Record(sw.Elapsed.TotalMilliseconds);

            logger.LogInformation("Workflow completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);

            ctx.State.RecoverySummary = new RecoverySummary(TotalRecoveries: ctx.State.Recoveries.Count,
                TotalFailures: ctx.State.Failures.Values.Sum(),
                DegradedMode: ctx.State.IsDegradedMode,
                Recoveries: ctx.State.Recoveries);

            return ctx.FinalResult ?? ctx.Insight ?? throw new InvalidOperationException($"Workflow completed without result for {input.Symbol}");
        }
        catch (Exception ex)
        {
            sw.Stop();

            workflowActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            workflowActivity?.AddException(ex);

            ObservabilityMetrics.WorkflowFailed.Add(1, new TagList
            {
                { "reason", "unhandled_exception" }
            });

            ObservabilityMetrics.WorkflowDuration.Record(sw.Elapsed.TotalMilliseconds);

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
        var stageSw = Stopwatch.StartNew();

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
        finally
        {
            stageSw.Stop();

            ObservabilityMetrics.StageDuration.Record(stageSw.Elapsed.TotalMilliseconds, new TagList
            {
                { "stage", stage.Stage.ToString() }
            });

            activity?.SetTag("stage.duration_ms", stageSw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task HandleStageFailureAsync(IMarketAgentStage stage, MarketAgentWorkflowContext ctx, Activity? workflowActivity, Stopwatch sw, Exception ex, CancellationToken ct)
    {
        scheduler.MarkFailed(stage.Stage);
        ctx.State.IncrementFailure(stage.Stage);

        var failure = await orchestrator.HandleFailureAsync(ctx, stage, ex, ct);

        if (failure.Strategy != RecoveryStrategy.Terminate)
        {
            ctx.State.Recoveries.Add(new RecoveryEvent(stage.Stage, failure.Strategy, failure.Reason ?? ex.Message, DateTimeOffset.UtcNow));
        }

        await ctx.EmitAsync(stage.Stage.ToString(), "recovery_applied", failure.Reason ?? failure.Strategy.ToString(), new
        {
            Stage = stage.Stage.ToString(),
            Strategy = failure.Strategy.ToString()
        }, ct);

        workflowActivity?.AddEvent(new ActivityEvent($"recovery:{failure.Strategy}", tags: new ActivityTagsCollection
        {
            { "stage", stage.Stage.ToString() },
            { "strategy", failure.Strategy.ToString() }
        }));

        if (failure.Strategy != RecoveryStrategy.Terminate)
        {
            var tags = new TagList
            {
                { "stage", stage.Stage.ToString() },
                { "strategy", failure.Strategy.ToString() }
            };

            if (failure.AlternateStage is not null)
            {
                tags.Add("alternate_stage", failure.AlternateStage.Value.ToString());
            }

            if (failure.FallbackStage is not null)
            {
                tags.Add("fallback_stage", failure.FallbackStage.Value.ToString());
            }

            ObservabilityMetrics.RecoveryApplied.Add(1, tags);
        }

        // Recovery execution
        switch (failure.Strategy)
        {
            case RecoveryStrategy.Fallback:
                {
                    if (failure.FallbackStage is null)
                    {
                        throw new InvalidOperationException($"Fallback strategy specified without fallback stage for {stage.Stage}");
                    }

                    var fallback = orchestrator.ResolveStage(failure.FallbackStage.Value) ?? throw new InvalidOperationException($"Fallback stage {failure.FallbackStage} could not be resolved");

                    logger.LogWarning("Executing fallback stage {FallbackStage}", failure.FallbackStage);

                    workflowActivity?.AddEvent(new ActivityEvent($"fallback:{failure.FallbackStage}"));

                    try
                    {
                        await fallback.ExecuteAsync(ctx, ct);

                        scheduler.MarkCompleted(failure.FallbackStage.Value);
                        ctx.State.IncrementRecovery(stage.Stage);

                        return;
                    }
                    catch (Exception fbEx)
                    {
                        scheduler.MarkFailed(failure.FallbackStage.Value);

                        throw new InvalidOperationException("Fallback execution failed", fbEx);
                    }
                }

            case RecoveryStrategy.Terminate:
                {
                    sw.Stop();

                    workflowActivity?.SetStatus(ActivityStatusCode.Error, failure.Reason);

                    ObservabilityMetrics.WorkflowFailed.Add(1, new TagList
                    {
                        { "reason", "stage_failure" }
                    });

                    ObservabilityMetrics.WorkflowDuration.Record(sw.Elapsed.TotalMilliseconds);

                    logger.LogCritical(ex, "Workflow terminating after stage failure");

                    ctx.Terminate(outcomeFactory.Safe(ctx, failure.Reason ?? "workflow_failure"));

                    ctx.WorkflowCancellationSource?.Cancel();

                    return;
                }

            case RecoveryStrategy.Skip:
                {
                    scheduler.MarkSkipped(stage.Stage);

                    logger.LogWarning("Skipping failed stage {Stage}", stage.Stage);

                    return;
                }

            case RecoveryStrategy.Degrade:
                {
                    logger.LogWarning("Entering degraded mode after stage failure {Stage}", stage.Stage);

                    ctx.State.IsDegradedMode = true;

                    return;
                }

            case RecoveryStrategy.Reroute:
                {
                    if (failure.AlternateStage is null)
                    {
                        throw new InvalidOperationException($"Reroute specified without alternate stage.");
                    }

                    var alternate = orchestrator.ResolveStage(failure.AlternateStage.Value) ?? throw new InvalidOperationException($"Alternate stage {failure.AlternateStage} could not be resolved");

                    logger.LogWarning("Rerouting {Stage} -> {Alternate}", stage.Stage, failure.AlternateStage);

                    workflowActivity?.SetTag("workflow.rerouted_from", stage.Stage.ToString());
                    workflowActivity?.SetTag("workflow.rerouted_to", failure.AlternateStage.Value.ToString());
                    workflowActivity?.AddEvent(new ActivityEvent($"reroute:{failure.AlternateStage}", tags: new ActivityTagsCollection
                    {
                        { "from", stage.Stage.ToString() },
                        { "to", failure.AlternateStage.Value.ToString() }
                    }));

                    await ctx.EmitAsync(stage.Stage.ToString(), "workflow_rerouted", $"Rerouted to {failure.AlternateStage}", new
                    {
                        From = stage.Stage.ToString(),
                        To = failure.AlternateStage.Value.ToString()
                    }, ct);

                    try
                    {
                        await alternate.ExecuteAsync(ctx, ct);

                        scheduler.MarkCompleted(failure.AlternateStage.Value);

                        ctx.State.IncrementRecovery(stage.Stage);

                        return;
                    }
                    catch (Exception rerouteEx)
                    {
                        scheduler.MarkFailed(failure.AlternateStage.Value);

                        throw new InvalidOperationException("Rerouted stage execution failed", rerouteEx);
                    }
                }

            default:
                {
                    throw new InvalidOperationException($"Unsupported recovery strategy: {failure.Strategy}");
                }
        }
    }

    private IAsyncPolicy ResolvePolicy(MarketAgentStage stage) => stage switch
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