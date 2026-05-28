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
    IMarketStageOrchestrator orchestrator)
{
    private readonly IAiPolicyRegistry _policyRegistry = policyRegistry;

    private readonly IReadOnlyList<IMarketAgentStage> _stages = [.. stages.OrderBy(x => x.Stage)];

    public async Task<AIInsightResult> RunAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

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

        try
        {
            foreach (var stage in _stages)
            {
                if (ctx.IsTerminated)
                {
                    logger.LogWarning("Workflow terminated before stage {Stage} for {Symbol}", stage.Stage, input.Symbol);

                    break;
                }

                ctx.CurrentStage = stage.Stage;

                var decision = await orchestrator.EvaluateExecutionAsync(ctx, stage, ct);

                if (!decision.Execute)
                {
                    logger.LogInformation("Skipping stage {Stage}. Reason: {Reason}", stage.Stage, decision.Reason);

                    continue;
                }

                logger.LogDebug("Executing stage {Stage} for {Symbol}", stage.Stage, input.Symbol);

                var policy = ResolvePolicy(stage.Stage);

                var pollyContext = new Context(stage.Stage.ToString());

                try
                {
                    await policy.ExecuteAsync(async (context, token) =>
                    {
                        context["emitter"] = ctx;

                        await stage.ExecuteAsync(ctx, token);

                    }, pollyContext, ct);
                }
                catch (Exception ex)
                {
                    var failure = await orchestrator.HandleFailureAsync(ctx, stage, ex, ct);

                    if (failure.UseFallback && failure.FallbackStage is not null)
                    {
                        var fallback = orchestrator.ResolveStage(failure.FallbackStage.Value);

                        if (fallback is not null)
                        {
                            logger.LogWarning("Executing fallback stage {FallbackStage}", failure.FallbackStage);

                            await fallback.ExecuteAsync(ctx, ct);

                            continue;
                        }
                    }

                    if (failure.TerminateWorkflow)
                    {
                        logger.LogCritical(ex, "Workflow terminating after stage failure");

                        return outcomeFactory.Safe(ctx, failure.Reason ?? "workflow_failure");
                    }

                    throw;
                }
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
