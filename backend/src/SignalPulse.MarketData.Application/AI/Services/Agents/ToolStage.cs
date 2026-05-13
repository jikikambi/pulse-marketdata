using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Plugins;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ToolStage(IQuoteInfoTool quoteTool,
    ILogger<ToolStage> logger,
    IWorkflowOutcomeFactory outcomeFactory) 
    : MarketAgentStageBase<ToolStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.Tooling;

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        var plan = ctx.Plan ?? throw new InvalidOperationException("Plan missing before tooling stage.");

        if (!plan.NeedTool)
        {
            Logger.LogDebug("Planner determined no tool required for {Symbol}", ctx.Input.Symbol);

            return;
        }

        Logger.LogInformation("Executing tool stage for {Symbol}. Tool: {Tool}", ctx.Input.Symbol, plan.Tool);

        if (plan.Tool != AgentConstants.ToolName)
        {
            Logger.LogWarning("Unauthorized tool requested for {Symbol}: {Tool}", ctx.Input.Symbol, plan.Tool);

            ctx.Terminate(outcomeFactory.Safe(ctx, "unauthorized_tool_request"));

            return;
        }

        try
        {
            var toolResult = await quoteTool.GetQuoteContextAsync(ctx.Input.Symbol);

            if (toolResult is null)
            {
                Logger.LogWarning("Tool returned null data for {Symbol}", ctx.Input.Symbol);

                ctx.Terminate(outcomeFactory.Safe(ctx, "missing_tool_data"));

                return;
            }

            var contextJson = JsonSerializer.Serialize(toolResult);

            ctx.ToolContextJson = contextJson;

            ctx.State.ToolUsed = true;
            ctx.State.ToolContextJson = contextJson;

            ctx.AddStep(AgentConstants.StepTool, ctx.Input.Symbol, contextJson);

            Logger.LogInformation("Tool stage completed successfully for {Symbol}", ctx.Input.Symbol);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Tool execution failed for {Symbol}", ctx.Input.Symbol);

            ctx.Terminate(outcomeFactory.Safe(ctx, "tool_call_failed"));
        }
    }
}