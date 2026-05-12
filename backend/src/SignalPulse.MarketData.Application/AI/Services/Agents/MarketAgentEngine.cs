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
    IQuoteInfoTool quoteTool,
    IRiskAgent riskAgent,
    IValidatorAgent validatorAgent,
    IConfidenceScoringAgent confidenceScoringAgent,
    IFinalDecisionAgent finalDecisionAgent,
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

        var ctx = new MarketAgentWorkflowContext
        {
            Input = input
        };

        // 1: Validate input

        if (input.Price <= 0 || input.Volume < 0)
        {
            logger.LogWarning("Invalid market data. Symbol: {Symbol}, Price: {Price}, Volume: {Volume}", input.Symbol, input.Price, input.Volume);

            return await Unsafe(ctx.State, "invalid_market_data");
        }

        // 2: Run planner without timeout and caching

        string planRaw;

        try
        {
            planRaw = await RunStage(ctx, MarketAgentStage.Planning, () => RunPlannerAsync(input, ct));

            logger.LogDebug("Planner completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Planner timeout for {Symbol} after {ElapsedMs}ms", input.Symbol, sw.ElapsedMilliseconds);

            return await Safe(ctx.State, "planner_timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Planner failed for {Symbol}", input.Symbol);

            return await Safe(ctx.State, "planner_failed");
        }

        ctx.State.PlanJson = planRaw;

        AddStep(ctx.State, AgentConstants.StepPlanner, input.Symbol, planRaw);

        // 3: Parse Plan

        PlannerResult? plan;

        try
        {
            plan = JsonSerializer.Deserialize<PlannerResult>(planRaw, AiJson.Options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize plan for {Symbol}", input.Symbol);

            return await Safe(ctx.State, "planner_deserialization_failed");
        }

        if (plan is null)
        {
            logger.LogWarning("Plan is null for {Symbol}", input.Symbol);

            return await Safe(ctx.State, "plan_is_null");
        }

        if (plan.Confidence < 0.5)
        {
            logger.LogDebug("Low confidence plan for {Symbol}: {Confidence}", input.Symbol, plan.Confidence);

            return await Safe(ctx.State, "low_confidence");
        }

        // 4: Fetch tool data if needed

        string? contextJson;

        try
        {
            contextJson = await RunStage(ctx, MarketAgentStage.Tooling, () => ExecuteToolStageAsync(ctx, plan, input));
        }
        catch (InvalidOperationException ex)
        {
            return await Safe(ctx.State, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool execution failed for {Symbol}", input.Symbol);

            return await Safe(ctx.State, "tool_call_failed");
        }

        // 5: Run reasoner with timeout

        AIInsightResult result;

        try
        {
            result = await RunStage(ctx, MarketAgentStage.Reasoning, () => RunReasonerAsync(input, contextJson, ct));

            logger.LogDebug("Reasoner completed in {ElapsedMs}ms for {Symbol}", sw.ElapsedMilliseconds, input.Symbol);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Reasoner timeout for {Symbol} after {ElapsedMs}ms", input.Symbol, sw.ElapsedMilliseconds);

            return await Safe(ctx.State, "reasoner_timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reasoner failed for {Symbol}", input.Symbol);

            return await Safe(ctx.State, "reasoner_failed");
        }

        AddStep(ctx.State, AgentConstants.StepReasoner, contextJson ?? "null", JsonSerializer.Serialize(result));

        // 6: Validate result

        var validation = await RunStage(ctx, MarketAgentStage.Validation, () => validatorAgent.ValidateAsync(input, result, ct));

        ctx.Validation = validation;

        if (!validation.IsValid)
        {
            logger.LogWarning("ValidatorAgent rejected insight for {Symbol}: {Reason}", input.Symbol, validation.Reason);

            return await Safe(ctx.State, "validation_failed");
        }

        // 7: Risk evaluation

        var risk = await RunStage(ctx, MarketAgentStage.RiskEvaluation, () => riskAgent.EvaluateAsync(input, result, ct));

        ctx.Risk = risk;

        AddStep(ctx.State, AgentConstants.StepRisker,
            JsonSerializer.Serialize(new RiskAuditInput(input.Symbol, input.ChangePercent, result.Volatility, result.Sentiment)),
            JsonSerializer.Serialize(risk));

        logger.LogInformation("RiskAgent evaluated {Symbol}. Risky: {IsRisky}, Level: {Level}, Reason: {Reason}", input.Symbol, risk.IsRisky, risk.Level, risk.Reason);

        if (risk.IsRisky)
        {
            logger.LogWarning("RiskAgent blocked execution for {Symbol}: {Reason}", input.Symbol, risk.Reason);

            return await Safe(ctx.State, "risk_threshold_exceeded");
        }

        // 8: Confidence scoring 

        var confidence = await RunStage(ctx, MarketAgentStage.Scoring, () => confidenceScoringAgent.ScoreAsync(ctx, ct));

        ctx.Confidence = confidence;

        ctx.State.Confidence = confidence;

        logger.LogInformation("ConfidenceScoringAgent evaluated {Symbol}. Score: {Score}, Level: {Level}", input.Symbol, confidence.Score, confidence.Level);  

        // 9: Final decision

        var decision = await RunStage(ctx, MarketAgentStage.Decision, () => finalDecisionAgent.DecideAsync(ctx, ct));

        ctx.FinalDecision = decision;

        ctx.State.FinalDecision = decision;

        logger.LogInformation("FinalDecisionAgent decided {Symbol}. Outcome: {Outcome}, Reason: {Reason}", input.Symbol, decision.Outcome, decision.Reason);

        // Workflow is now complete

        ctx.State.Completed = true;

        ctx.State.StageResults = ctx.StageResults;

        await Persist(key, ctx.State);

        sw.Stop();

        logger.LogInformation("Agent completed successfully in {TotalMs}ms for {Symbol}. ToolUsed: {ToolUsed}, Completed: {Completed}",
            sw.ElapsedMilliseconds, input.Symbol, ctx.State.ToolUsed, ctx.State.Completed);

        // 10: Final governance enforcement

        if (decision.Outcome != DecisionOutcome.Approved)
        {
            return await Safe(ctx.State, $"decision_{decision.Outcome.ToString().ToLowerInvariant()}");
        }

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

    private async Task<T> RunStage<T>(MarketAgentWorkflowContext ctx, MarketAgentStage stage, Func<Task<T>> action)
    {
        var started = DateTimeOffset.UtcNow;

        var sw = Stopwatch.StartNew();

        try
        {
            logger.LogDebug("Starting stage {Stage} for {Symbol}", stage, ctx.Input.Symbol);

            var result = await action();

            sw.Stop();

            ctx.StageResults.Add(new StageExecutionResult(
                Stage: stage.ToString(),
                Success: true,
                DurationMs: sw.ElapsedMilliseconds,
                Error: null,
                StartedAt: started,
                CompletedAt: DateTimeOffset.UtcNow));

            logger.LogDebug("Completed stage {Stage} in {ElapsedMs}ms for {Symbol}", stage, sw.ElapsedMilliseconds, ctx.Input.Symbol);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();

            ctx.StageResults.Add(new StageExecutionResult(
                Stage: stage.ToString(),
                Success: false,
                DurationMs: sw.ElapsedMilliseconds,
                Error: ex.Message,
                StartedAt: started,
                CompletedAt: DateTimeOffset.UtcNow));

            logger.LogError(ex, "Stage {Stage} failed after {ElapsedMs}ms for {Symbol}", stage, sw.ElapsedMilliseconds, ctx.Input.Symbol);

            throw;
        }
    }

    private async Task<string?> ExecuteToolStageAsync(MarketAgentWorkflowContext ctx, PlannerResult plan, QuoteInsightInput input)
    {
        if (!plan.NeedTool)
        {
            return null;
        }

        return await RunStage(ctx, MarketAgentStage.Tooling, async () =>
        {
            if (plan.Tool != AgentConstants.ToolName)
            {

                throw new InvalidOperationException("unauthorized_tool_request");
            }

            var toolResult = await quoteTool.GetQuoteContextAsync(input.Symbol) ?? throw new InvalidOperationException("missing_tool_data");

            var contextJson = JsonSerializer.Serialize(toolResult);

            ctx.State.ToolUsed = true;
            ctx.State.ToolContextJson = contextJson;

            AddStep(ctx.State, AgentConstants.StepTool, input.Symbol, contextJson);

            return contextJson;
        });
    }
}