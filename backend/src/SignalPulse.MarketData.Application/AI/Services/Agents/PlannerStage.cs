using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Retry;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Globalization;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class PlannerStage(IKernelInvoker kernelInvoker,
    IAgentStateStore store,
    IAsyncPolicy<string> retryPolicy,
    ILogger<PlannerStage> logger,
    IWorkflowOutcomeFactory outcomeFactory) 
    : MarketAgentStageBase<PlannerStage>(logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public override MarketAgentStage Stage => MarketAgentStage.Planning;

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var input = ctx.Input;

        var normalizedPercent = Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture);

        var cacheKey = $"{input.Symbol}:{normalizedPercent}";

        var cachedPlan = await store.GetPlanCacheAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedPlan))
        {
            Logger.LogDebug("Planner cache hit for {Symbol}. CacheKey: {CacheKey}", input.Symbol, cacheKey);

            ctx.PlanRaw = cachedPlan;
            return;
        }

        Logger.LogDebug("Planner cache miss for {Symbol}. CacheKey: {CacheKey}. Making LLM call...", input.Symbol, cacheKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        cts.CancelAfter(Timeout);

        try
        {
            var result = await retryPolicy.ExecuteAsync(async () =>
            {
                return await kernelInvoker.InvokeAsync(AgentConstants.PlannerFunction, new KernelArguments
                {
                    ["symbol"] = input.Symbol,
                    ["price"] = input.Price,
                    ["changePercent"] = input.ChangePercent,
                    ["volume"] = input.Volume,
                    ["correlationId"] = input.CorrelationId
                }, cts.Token);
            });

            var planRaw = result.ToString();

            if (string.IsNullOrWhiteSpace(planRaw))
            {
                throw new InvalidOperationException($"Planner returned empty response for {input.Symbol}");
            }

            await store.SetPlanCacheAsync(cacheKey, planRaw);

            Logger.LogDebug("Planner result cached for {Symbol}. CacheKey: {CacheKey}, Duration: 30s", input.Symbol, cacheKey);

            ctx.PlanRaw = planRaw;
            ctx.State.PlanJson = planRaw;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Logger.LogWarning("Planner timed out after {TimeoutMs}ms for {Symbol}", Timeout.TotalMilliseconds, input.Symbol);

            var fallback = outcomeFactory.Safe(ctx, "planner_timeout");

            ctx.Terminate(fallback);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Planner failed for {Symbol}", input.Symbol);

            var fallback = outcomeFactory.Safe(ctx, "planner_failed");

            ctx.Terminate(fallback);
        }

        ctx.AddStep(AgentConstants.StepPlanner, input.Symbol, ctx.PlanRaw!);
    }
}