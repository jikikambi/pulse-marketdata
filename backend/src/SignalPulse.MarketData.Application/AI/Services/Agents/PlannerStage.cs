using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class PlannerStage(IPlannerAgentResolver plannerResolver,
    IAgentStateStore store,
    ILogger<PlannerStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<PlannerStage>(logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public override MarketAgentStage Stage => MarketAgentStage.Planning;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [MarketAgentStage.ValidationInput];

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var input = ctx.Input;

        var normalizedPercent = Math.Round(input.ChangePercent, 2).ToString(CultureInfo.InvariantCulture);

        var cacheKey = $"{input.Symbol}:{normalizedPercent}";

        await ctx.EmitAsync(Stage.ToString(), "planner_started", "Planner stage execution started", new
        {
            CacheKey = cacheKey,
            input.Symbol,
            input.ChangePercent
        }, ct);

        try
        {
            var cachedPlan = await store.GetPlanCacheAsync(cacheKey);

            if (!string.IsNullOrWhiteSpace(cachedPlan))
            {
                Logger.LogDebug("Planner cache hit for {Symbol}. CacheKey: {CacheKey}", input.Symbol, cacheKey);

                ctx.PlanRaw = cachedPlan;

                await ctx.EmitAsync(Stage.ToString(), "planner_cache_hit", "Using cached planner response", new
                {
                    CacheKey = cacheKey
                }, ct);

                return;
            }

            await ctx.EmitAsync(Stage.ToString(), "planner_cache_miss", "Planner cache miss", new
            {
                CacheKey = cacheKey
            }, ct);

            Logger.LogDebug("Planner cache miss for {Symbol}. CacheKey: {CacheKey}. Making LLM call...", input.Symbol, cacheKey);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            var primary = plannerResolver.GetPrimary();
            var fallback = plannerResolver.GetFallback();

            string? planRaw;

            try
            {
                planRaw = await primary.GenerateAsync(input, ctx, Stage, cts.Token);
            }
            catch (Exception ex) when (fallback is not null)
            {
                Logger.LogWarning(ex, "Primary planner agent failed for {Symbol}. Switching to fallback agent.", input.Symbol);

                await ctx.EmitAsync(Stage.ToString(), "planner_fallback", $"Switching to fallback planner agent [{fallback.Name}]", null, cts.Token);

                planRaw = await fallback.GenerateAsync(input, ctx, Stage, cts.Token);

                ctx.State.AlternateAgentsUsed[Stage] = fallback.Name;

                ObservabilityMetrics.Agent.AlternateAgentUsed.Add(1, new TagList
                {
                    { "stage", Stage.ToString() },
                    { "agent", fallback.Name }
                });
            }

            if (string.IsNullOrWhiteSpace(planRaw))
            {
                Logger.LogWarning("Planner returned empty response for {Symbol}", input.Symbol);

                await ctx.EmitAsync(Stage.ToString(), "planner_empty_response", "Planner returned empty response", null, cts.Token);

                ctx.Terminate(outcomeFactory.Safe(ctx, "planner_empty_response"));

                return;
            }

            await store.SetPlanCacheAsync(cacheKey, planRaw);

            Logger.LogDebug("Planner result cached for {Symbol}. CacheKey: {CacheKey}, Duration: 30s", input.Symbol, cacheKey);

            ctx.PlanRaw = planRaw;
            ctx.State.PlanJson = planRaw;

            await ctx.EmitAsync(Stage.ToString(), "planner_cached", "Planner response cached", new
            {
                CacheKey = cacheKey
            }, cts.Token);

            try
            {
                var preview = JsonSerializer.Deserialize<PlannerResult>(planRaw, AiJson.Options);

                if (preview is not null)
                {
                    await ctx.EmitAsync(Stage.ToString(), "planner_confidence", $"Planner confidence: {preview.Confidence}", new
                    {
                        preview.Confidence,
                        preview.NeedTool,
                        preview.Tool
                    }, cts.Token);

                    await ctx.EmitAsync(Stage.ToString(), "planner_tool_decision",
                        preview.NeedTool ? "Planner requested tool" : "Planner skipped tool", new
                        {
                            preview.Tool
                        }, cts.Token);
                }
            }
            catch
            {
                // Ignore parse failures here.
                // PlanParsingStage owns actual validation.
            }

            ctx.AddStep(AgentConstants.StepPlanner, input.Symbol, planRaw);

            Logger.LogInformation("Planner completed successfully for {Symbol}", input.Symbol);

        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            Logger.LogWarning("Planner timed out after {TimeoutMs}ms for {Symbol}", Timeout.TotalMilliseconds, input.Symbol);

            await ctx.EmitAsync(Stage.ToString(), "planner_timeout", ex.Message, null, ct);

            var fallback = outcomeFactory.Safe(ctx, "planner_timeout");

            ctx.Terminate(fallback);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Planner failed for {Symbol}", input.Symbol);

            await ctx.EmitAsync(Stage.ToString(), "planner_failed", ex.Message, new
            {
                Exception = ex.GetType().Name
            }, ct);

            var fallback = outcomeFactory.Safe(ctx, "planner_failed");

            ctx.Terminate(fallback);
        }
    }
}