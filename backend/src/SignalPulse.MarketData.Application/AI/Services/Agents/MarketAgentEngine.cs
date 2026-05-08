using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly.Retry;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Policies;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class MarketAgentEngine(IKernelInvoker kernelInvoker,
    QuoteInfoPlugin quotePlugin,
    IAgentStateStore store,
    ILogger<MarketAgentEngine> logger)
{
    private readonly AsyncRetryPolicy _retryPolicy = AiRetryPolicies.Create();

    private readonly TimeSpan _plannerTimeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _reasonerTimeout = TimeSpan.FromSeconds(5);

    public async Task<AIInsightResult> RunAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var key = $"agent:{input.Symbol}:{input.CorrelationId}";

        logger.LogInformation(
            "Agent starting. Symbol: {Symbol}, CorrelationId: {CorrelationId}, Price: {Price}, Change%: {ChangePercent}",
            input.Symbol, input.CorrelationId, input.Price, input.ChangePercent);

        var state = new MarketAgentState
        {
            CorrelationId = input.CorrelationId,
            Symbol = input.Symbol
        };

        // 1: Validate input

        if (input.Price <= 0 || input.Volume < 0)
        {
            logger.LogWarning("Invalid market data. Symbol: {Symbol}, Price: {Price}, Volume: {Volume}", input.Symbol, input.Price, input.Volume);

            return await Unsafe(state, "invalid_market_data");
        }

        // 2: Run planner without timeout and caching

        string planRaw;

        try
        {
            planRaw = await RunPlannerAsync(input, ct);
            logger.LogDebug("Planner completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Planner timeout for {Symbol} after {ElapsedMs}ms", input.Symbol, sw.ElapsedMilliseconds);

            return await Safe(state, "planner_timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Planner failed for {Symbol}", input.Symbol);

            return await Safe(state, "planner_failed");
        }

        state.PlanJson = planRaw;

        AddStep(state, AgentConstants.StepPlanner, input.Symbol, planRaw);

        // 3: Parse Plan

        PlannerResult? plan;

        try
        {
            plan = JsonSerializer.Deserialize<PlannerResult>(planRaw, AiJson.Options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize plan for {Symbol}", input.Symbol);

            return await Safe(state, "planner_deserialization_failed");
        }

        if (plan is null)
        {
            logger.LogWarning("Plan is null for {Symbol}", input.Symbol);

            return await Safe(state, "plan_is_null");
        }

        if (plan.Confidence < 0.5)
        {
            logger.LogDebug("Low confidence plan for {Symbol}: {Confidence}", input.Symbol, plan.Confidence);

            return await Safe(state, "low_confidence");
        }

        // 4: Fetch tool data if needed

        string? contextJson = null;

        if (plan.NeedTool)
        {
            if (plan.Tool != AgentConstants.ToolName)
            {
                logger.LogWarning("Unauthorized tool request for {Symbol}: {Tool}", input.Symbol, plan.Tool);

                return await Safe(state, "unauthorized_tool_request");
            }

            try
            {
                var toolResult = await quotePlugin.GetQuoteContextAsync(input.Symbol);

                if (toolResult is null)
                {
                    logger.LogWarning("Tool returned null for {Symbol}", input.Symbol);

                    return await Safe(state, "missing_tool_data");
                }

                contextJson = JsonSerializer.Serialize(toolResult);

                state.ToolUsed = true;
                state.ToolContextJson = contextJson;

                AddStep(state, AgentConstants.StepTool, input.Symbol, contextJson);

                logger.LogDebug("Tool call completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tool call failed for {Symbol}", input.Symbol);

                return await Safe(state, "tool_call_failed");
            }
        }

        // 5: Run reasoner with timeout

        AIInsightResult result;

        try
        {
            result = await RunReasonerAsync(input, contextJson, ct);

            logger.LogDebug("Reasoner completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Reasoner timeout for {Symbol} after {ElapsedMs}ms", input.Symbol, sw.ElapsedMilliseconds);

            return await Safe(state, "reasoner_timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reasoner failed for {Symbol}", input.Symbol);

            return await Safe(state, "reasoner_failed");
        }

        // 6: Validate result

        if (!IsValid(result))
        {
            logger.LogWarning("Invalid LLM output for {Symbol}: Sentiment={Sentiment}", input.Symbol, result.Sentiment);

            return await Safe(state, "invalid_llm_output");
        }

        AddStep(state, AgentConstants.StepReasoner, contextJson ?? "null", JsonSerializer.Serialize(result));

        state.Completed = true;

        await Persist(key, state);

        sw.Stop();

        logger.LogInformation("Agent completed successfully in {TotalMs}ms for {Symbol}. ToolUsed: {ToolUsed}, Completed: {Completed}",
            sw.ElapsedMilliseconds, input.Symbol, state.ToolUsed, state.Completed);

        return result;
    }

    private async Task<string> RunPlannerAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var normalizedPercent = Math.Round(input.ChangePercent, 2)
            .ToString(CultureInfo.InvariantCulture);

        var cacheKey = $"{input.Symbol}:{normalizedPercent}";

        var cachedPlan = await store.GetPlanCacheAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedPlan))
        {
            logger.LogDebug("Planner cache hit for {Symbol}. CacheKey: {CacheKey}", input.Symbol, cacheKey);

            return cachedPlan;
        }

        logger.LogDebug("Planner cache miss for {Symbol}. CacheKey: {CacheKey}. Making LLM call...", input.Symbol, cacheKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_plannerTimeout);

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
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

            var plan = result.ToString();

            if (string.IsNullOrWhiteSpace(plan))
            {
                throw new InvalidOperationException($"Planner returned empty response for {input.Symbol}");
            }

            await store.SetPlanCacheAsync(cacheKey, plan);

            logger.LogDebug("Planner result cached for {Symbol}. CacheKey: {CacheKey}, Duration: 30s", input.Symbol, cacheKey);

            return plan;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Planner timed out after {TimeoutMs}ms for {Symbol}", _plannerTimeout.TotalMilliseconds, input.Symbol);
            throw;
        }
    }

    private async Task<AIInsightResult> RunReasonerAsync(QuoteInsightInput input, string? context, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        cts.CancelAfter(_reasonerTimeout);

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await kernelInvoker.InvokeAsync(AgentConstants.ReasonerFunction, new KernelArguments
                {
                    ["symbol"] = input.Symbol,
                    ["price"] = input.Price,
                    ["changePercent"] = input.ChangePercent,
                    ["volume"] = input.Volume,
                    ["context"] = context ?? "null",
                    ["correlationId"] = input.CorrelationId
                }, cts.Token);
            });

            return JsonSerializer.Deserialize<AIInsightResult>(result.ToString(), AiJson.Options)!;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Reasoner timed out after {TimeoutMs}ms for {Symbol}", _reasonerTimeout.TotalMilliseconds, input.Symbol);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Reasoner returned invalid JSON for {Symbol}", input.Symbol);

            throw new InvalidOperationException("Reasoner returned invalid JSON", ex);
        }
    }

    private static bool IsValid(AIInsightResult result) =>
        result is not null &&
        !string.IsNullOrWhiteSpace(result.Rationale) &&
        result.Sentiment is SentimentType.Bullish or SentimentType.Bearish or SentimentType.Neutral;

    private async Task<AIInsightResult> Safe(MarketAgentState state, string reason)
    {
        AddStep(state, AgentConstants.StepSafe, reason, "");

        logger.LogWarning("Safe fallback triggered for {Symbol}: {Reason}", state.Symbol, reason);

        return new(SentimentType.Neutral, DirectionType.Sideways, VolatilityType.Low, $"safe_fallback: {reason}");
    }

    private async Task<AIInsightResult> Unsafe(MarketAgentState state, string reason)
    {
        AddStep(state, AgentConstants.StepUnsafe, reason, "");

        logger.LogError("Unsafe exit for {Symbol}: {Reason}", state.Symbol, reason);

        return new(SentimentType.Neutral, DirectionType.Sideways, VolatilityType.High, $"rejected_input: {reason}");
    }

    private static void AddStep(MarketAgentState state, string name, string input, string output)
    {
        state.Steps.Add(new AgentStep(name, input, output, DateTimeOffset.UtcNow));
        state.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private Task Persist(string key, MarketAgentState state)
        => store.SetAsync(key, state);
}