using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public abstract class MarketAgentStageBase<TStage>(ILogger<TStage> logger) : IMarketAgentStage
{
    private static readonly ActivitySource ActivitySource = new("SignalPulse.MarketAgent");
    private static readonly Meter Meter = new("SignalPulse.MarketAgent");
    private static readonly Histogram<double> StageDuration = Meter.CreateHistogram<double>("marketagent.stage.duration");
    private static readonly Counter<long> StageSuccessCounter = Meter.CreateCounter<long>("marketagent.stage.success");
    private static readonly Counter<long> StageFailureCounter = Meter.CreateCounter<long>("marketagent.stage.failure");
    private static readonly Counter<long> StageDegradedCounter = Meter.CreateCounter<long>("marketagent.stage.degraded");

    protected readonly ILogger<TStage> Logger = logger;
    public abstract MarketAgentStage Stage { get; }

    public virtual IReadOnlyCollection<MarketAgentStage> DependsOn => [];

    public async Task ExecuteAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;

        var sw = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity($"MarketAgent.{Stage}", ActivityKind.Internal);

        activity?.SetTag("market.stage", Stage.ToString());
        activity?.SetTag("workflow.symbol", ctx.Input.Symbol);
        activity?.SetTag("workflow.correlation_id", ctx.Input.CorrelationId);
        activity?.SetTag("workflow.degraded_mode", ctx.State.IsDegradedMode);
        activity?.SetTag("workflow.retry_count", ctx.State.RetryCount);
        activity?.SetTag("workflow.tool_used", ctx.State.ToolUsed);

        try
        {
            Logger.LogDebug("Starting stage {Stage} for {Symbol}", Stage, ctx.Input.Symbol);

            await ctx.EmitAsync(Stage.ToString(), "stage_started", $"Stage {Stage} started", new
            {
                ctx.Input.Symbol
            }, ct);

            await ExecuteInternalAsync(ctx, ct);

            sw.Stop();

            StageDuration.Record(sw.Elapsed.TotalMilliseconds, new TagList
            {
                { "stage", Stage.ToString() },
                { "status", "success" }
            });

            StageSuccessCounter.Add(1, new TagList
            {
                { "stage", Stage.ToString() }
            });

            if (ctx.State.IsDegradedMode)
            {
                StageDegradedCounter.Add(1, new TagList
                {
                    { "stage", Stage.ToString() }
                });
            }

            activity?.SetTag("stage.duration_ms", sw.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);

            ctx.StageResults.Add(new StageExecutionResult(Stage.ToString(), true, sw.ElapsedMilliseconds, null, started, DateTimeOffset.UtcNow));

            await ctx.EmitAsync(Stage.ToString(), "stage_completed", $"Stage {Stage} completed", new
            {
                DurationMs = sw.ElapsedMilliseconds
            }, ct);

            Logger.LogDebug("Completed stage {Stage} in {ElapsedMs}ms for {Symbol}", Stage, sw.ElapsedMilliseconds, ctx.Input.Symbol);
        }
        catch (Exception ex)
        {
            sw.Stop();

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            activity?.AddException(ex);

            activity?.SetTag("stage.exception_type", ex.GetType().Name);

            StageFailureCounter.Add(1, new TagList
            {
                { "stage", Stage.ToString() }
            });

            StageDuration.Record(sw.Elapsed.TotalMilliseconds, new TagList
            {
                { "stage", Stage.ToString() },
                { "status", "failed" }
            });

            ctx.StageResults.Add(new StageExecutionResult(Stage.ToString(), false, sw.ElapsedMilliseconds, ex.Message, started, DateTimeOffset.UtcNow));

            await ctx.EmitAsync(Stage.ToString(), "stage_failed", ex.Message, new
            {
                Exception = ex.GetType().Name,
                DurationMs = sw.ElapsedMilliseconds
            }, ct);

            Logger.LogError(ex, "Stage {Stage} failed after {ElapsedMs}ms for {Symbol}", Stage, sw.ElapsedMilliseconds, ctx.Input.Symbol);

            throw;
        }
    }

    protected abstract Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct);

    protected Task EmitAsync(MarketAgentWorkflowContext ctx, string type, string message, object? metadata = null, CancellationToken ct = default)
        => ctx.EmitAsync(Stage.ToString(), type, message, metadata, ct);
}
