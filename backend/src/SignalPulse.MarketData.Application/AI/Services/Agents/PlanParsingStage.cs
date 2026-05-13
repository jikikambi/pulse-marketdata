using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class PlanParsingStage(ILogger<PlanParsingStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<PlanParsingStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.PlanParsing;

    protected override Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.PlanRaw))
        {
            throw new InvalidOperationException("PlanRaw is required before parsing stage.");
        }

        PlannerResult? plan;

        try
        {
            plan = JsonSerializer.Deserialize<PlannerResult>(ctx.PlanRaw, AiJson.Options);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deserialize planner result for {Symbol}", ctx.Input.Symbol);

            var fallback = outcomeFactory.Safe(ctx, "planner_deserialization_failed");

            ctx.Terminate(fallback);

            return Task.CompletedTask;
        }

        if (plan is null)
        {
            Logger.LogWarning("Planner returned null plan for {Symbol}", ctx.Input.Symbol);

            var fallback = outcomeFactory.Safe(ctx, "plan_is_null");

            ctx.Terminate(fallback);

            return Task.CompletedTask;
        }

        if (plan.Confidence < 0.5)
        {
            Logger.LogWarning("Planner confidence too low for {Symbol}. Confidence: {Confidence}", ctx.Input.Symbol, plan.Confidence);

            var fallback = outcomeFactory.Safe(ctx, "low_confidence");

            ctx.Terminate(fallback);

            return Task.CompletedTask;
        }

        ctx.Plan = plan;

        Logger.LogInformation("Planner result parsed successfully for {Symbol}. Confidence: {Confidence}, NeedTool: {NeedTool}", ctx.Input.Symbol, plan.Confidence, plan.NeedTool);

        return Task.CompletedTask;
    }
}