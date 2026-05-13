using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Diagnostics;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public abstract class MarketAgentStageBase<TStage>(ILogger<TStage> logger) : IMarketAgentStage
{
    protected readonly ILogger<TStage> Logger = logger;
    public abstract MarketAgentStage Stage { get; }

    public async Task ExecuteAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;

        var sw = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Starting stage {Stage} for {Symbol}", Stage, ctx.Input.Symbol);

            await ExecuteInternalAsync(ctx, ct);

            sw.Stop();

            ctx.StageResults.Add(new StageExecutionResult(Stage.ToString(), true, sw.ElapsedMilliseconds, null, started, DateTimeOffset.UtcNow));

            Logger.LogDebug("Completed stage {Stage} in {ElapsedMs}ms for {Symbol}", Stage, sw.ElapsedMilliseconds, ctx.Input.Symbol);
        }
        catch (Exception ex)
        {
            sw.Stop();

            ctx.StageResults.Add(new StageExecutionResult(Stage.ToString(), false, sw.ElapsedMilliseconds, ex.Message, started, DateTimeOffset.UtcNow));

            Logger.LogError(ex, "Stage {Stage} failed after {ElapsedMs}ms for {Symbol}", Stage, sw.ElapsedMilliseconds, ctx.Input.Symbol);

            throw;
        }
    }

    protected abstract Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct);
}
