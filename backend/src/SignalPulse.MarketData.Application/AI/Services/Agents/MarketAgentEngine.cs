using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class MarketAgentEngine(Kernel kernel,
    QuoteInfoPlugin quotePlugin,
    IAgentStateStore store)
{
    private readonly string _promptPath = Path.Combine(AppContext.BaseDirectory, AgentConstants.PromptPath);

    public async Task<AIInsightResult> RunAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var key = $"agent:{input.Symbol}:{input.CorrelationId}";

        var state = new MarketAgentState
        {
            CorrelationId = input.CorrelationId,
            Symbol = input.Symbol
        };

        await Persist(key, state);

        if (input.Price <= 0 || input.Volume < 0)
            return await Unsafe(key, state, "invalid_market_data");

        var planRaw = await RunPlannerAsync(input, ct);

        state.PlanJson = planRaw;
        AddStep(state, AgentConstants.StepPlanner, input.Symbol, planRaw);

        await Persist(key, state);

        PlannerResult? plan;

        try
        {
            plan = JsonSerializer.Deserialize<PlannerResult>(planRaw);
        }
        catch
        {
            return await Safe(key, state, "planner_deserialization_failed");
        }

        if (plan is null || plan.Confidence < 0.5)
            return await Safe(key, state, "low_confidence");

        string? contextJson = null;

        if (plan.NeedTool)
        {
            if (plan.Tool != AgentConstants.ToolName)
                return await Safe(key, state, "unauthorized_tool_request");

            var toolResult = await quotePlugin.GetQuoteContextAsync(input.Symbol);

            if (toolResult is null)
                return await Safe(key, state, "missing_tool_data");

            QuoteContextDto? context;

            try
            {
                context = JsonSerializer.Deserialize<QuoteContextDto>(JsonSerializer.Serialize(toolResult));
            }
            catch
            {
                return await Safe(key, state, "tool_deserialization_failed");
            }

            contextJson = JsonSerializer.Serialize(context);

            state.ToolUsed = true;
            state.ToolContextJson = contextJson;

            AddStep(state, AgentConstants.StepTool, input.Symbol, contextJson);

            await Persist(key, state);
        }

        AIInsightResult result;

        try
        {
            result = await RunReasonerAsync(input, contextJson, ct);
        }
        catch
        {
            return await Safe(key, state, "reasoner_failed");
        }

        AddStep(state, AgentConstants.StepReasoner, contextJson ?? "null",
            JsonSerializer.Serialize(result));

        await Persist(key, state);

        if (!IsValid(result))
            return await Safe(key, state, "invalid_llm_output");

        state.Completed = true;

        await Persist(key, state);

        return result;
    }

    private async Task<string> RunPlannerAsync(QuoteInsightInput input, CancellationToken ct)
    {
        var plugin = kernel.CreatePluginFromPromptDirectory(_promptPath);

        var result = await kernel.InvokeAsync(plugin[AgentConstants.PlannerFunction], new KernelArguments
        {
            ["symbol"] = input.Symbol,
            ["price"] = input.Price,
            ["changePercent"] = input.ChangePercent,
            ["volume"] = input.Volume
        }, ct);

        return result.ToString();
    }

    private async Task<AIInsightResult> RunReasonerAsync(QuoteInsightInput input, string? context, CancellationToken ct)
    {
        var plugin = kernel.CreatePluginFromPromptDirectory(_promptPath);

        var result = await kernel.InvokeAsync(plugin[AgentConstants.ReasonerFunction], new KernelArguments
        {
            ["symbol"] = input.Symbol,
            ["price"] = input.Price,
            ["changePercent"] = input.ChangePercent,
            ["volume"] = input.Volume,
            ["context"] = context ?? "null"
        }, ct);

        try
        {
            return JsonSerializer.Deserialize<AIInsightResult>(result.ToString())!;
        }
        catch
        {
            throw new InvalidOperationException("Reasoner returned invalid JSON");
        }
    }

    private static bool IsValid(AIInsightResult result) =>
        result is not null &&
        !string.IsNullOrWhiteSpace(result.Rationale) &&
        result.Sentiment is "bullish" or "bearish" or "neutral";

    private async Task<AIInsightResult> Safe(string key, MarketAgentState state, string reason)
    {
        AddStep(state, AgentConstants.StepSafe, reason, "");

        await Persist(key, state);

        return new("neutral", "sideways", "low", $"safe_fallback: {reason}");
    }

    private async Task<AIInsightResult> Unsafe(string key, MarketAgentState state, string reason)
    {
        AddStep(state, AgentConstants.StepUnsafe, reason, "");

        await Persist(key, state);

        return new("neutral", "sideways", "high", $"rejected_input: {reason}");
    }

    private static void AddStep(MarketAgentState state, string name, string input, string output)
    {
        state.Steps.Add(new AgentStep(name, input, output, DateTimeOffset.UtcNow));
        state.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private Task Persist(string key, MarketAgentState state)
        => store.SetAsync(key, state);
}