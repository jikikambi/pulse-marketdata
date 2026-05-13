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

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        if (ctx.Insight is null)
        {
            throw new InvalidOperationException("Insight is required before validation stage.");
        }

        var validation = await validatorAgent.ValidateAsync(ctx.Input, ctx.Insight, ct);

        ctx.Validation = validation;

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