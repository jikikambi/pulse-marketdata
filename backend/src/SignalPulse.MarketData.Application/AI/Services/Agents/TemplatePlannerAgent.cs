using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class TemplatePlannerAgent : IPlannerExecutionAgent
{
    public string Name => "template";

    public async Task<string?> GenerateAsync(QuoteInsightInput input, MarketAgentWorkflowContext ctx, MarketAgentStage stage, CancellationToken ct)
    {
        await ctx.EmitAsync(stage.ToString(), "planner_template", "Using template planner", new
        {
            input.Symbol,
            input.ChangePercent
        }, ct);

        var needTool = Math.Abs(input.ChangePercent) > 3m;

        var result = new PlannerResult(needTool,
            needTool ? AgentConstants.ToolName : null!,
            needTool ? 0.85 : 0.95,
            needTool ? "Large price movement requires historical context." : "Current market data sufficient.");

        return JsonSerializer.Serialize(result);
    }
}