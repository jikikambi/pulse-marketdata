using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ValidationStage(IValidatorAgent validatorAgent,
    ILogger<ValidationStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<ValidationStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.Validation;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [MarketAgentStage.Reasoning];

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        await ctx.EmitAsync(Stage.ToString(), "validation_started", "Validation stage started", null, ct);

        if (ctx.Insight is null)
        {
            Logger.LogWarning("{Insight} is required before validation stage", ctx.Insight);
            await ctx.EmitAsync(Stage.ToString(), "validation_failed", "Insight is required before validation stage", null, ct);
            return;
        }

        var validation = await validatorAgent.ValidateAsync(ctx.Input, ctx.Insight, ct);

        ctx.Validation = validation;

        await ctx.EmitAsync(Stage.ToString(),
            validation.IsValid ? "validation_passed" : "validation_failed", validation.Reason, new
            {
                validation.IsValid,
                Severity = validation.Severity.ToString()
            }, ct);

        if (!validation.IsValid)
        {
            Logger.LogWarning("Validation failed for {Symbol}: {Reason}", ctx.Input.Symbol, validation.Reason);

            var fallback = outcomeFactory.Safe(ctx, "validation_failed");

            ctx.Terminate(fallback);

            return;
        }

        Logger.LogInformation("Validation passed for {Symbol}", ctx.Input.Symbol);
    }   
}