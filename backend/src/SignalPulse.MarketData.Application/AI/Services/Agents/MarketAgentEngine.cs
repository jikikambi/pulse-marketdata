using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using System.Diagnostics;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class MarketAgentEngine(IEnumerable<IMarketAgentStage> stages,
    ILogger<MarketAgentEngine> logger,
    IWorkflowOutcomeFactory outcomeFactory)
{
    private readonly IReadOnlyList<IMarketAgentStage> _stages = [.. stages.OrderBy(x => x.Stage)];

    public async Task<AIInsightResult> RunAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var key = $"agent:{input.Symbol}:{input.CorrelationId}";

        logger.LogInformation("Starting market agent workflow for {Symbol}", input.Symbol);

        var ctx = new MarketAgentWorkflowContext
        {
            Input = input,
            State = new MarketAgentState
            {
                Symbol = input.Symbol,
                CorrelationId = input.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        try
        {
            foreach (var stage in _stages)
            {
                if (ctx.IsTerminated)
                {
                    logger.LogWarning("Workflow terminated before stage {Stage} for {Symbol}", stage.Stage, input.Symbol);

                    break;
                }

                logger.LogDebug("Executing stage {Stage} for {Symbol}", stage.Stage, input.Symbol);

                await stage.ExecuteAsync(ctx, ct);
            }

            sw.Stop();

            logger.LogInformation("Workflow completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);

            return ctx.FinalResult ?? ctx.Insight ?? throw new InvalidOperationException($"Workflow completed without result for {input.Symbol}");
        }
        catch (Exception ex)
        {
            sw.Stop();

            logger.LogCritical(ex, "Unhandled workflow failure for {Symbol} after {ElapsedMs}ms", input.Symbol, sw.ElapsedMilliseconds);

            return outcomeFactory.Safe(ctx, "workflow_unhandled_exception");
        }
    }
}
