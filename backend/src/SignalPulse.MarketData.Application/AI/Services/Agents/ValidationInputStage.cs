using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ValidationInputStage(ILogger<ValidationInputStage> logger,
    IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<ValidationInputStage>(logger)
{
    public override MarketAgentStage Stage => MarketAgentStage.ValidationInput;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [];

    protected override Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        Logger.LogDebug("Validating input for {Symbol}. Price: {Price}, Volume: {Volume}, ChangePercent: {ChangePercent}", ctx.Input.Symbol, ctx.Input.Price, ctx.Input.Volume, ctx.Input.ChangePercent);

        if (ctx.Input.Price <= 0)
        {
            Logger.LogWarning("Invalid price detected for {Symbol}. Price: {Price}", ctx.Input.Symbol, ctx.Input.Price);

            ctx.Terminate(outcomeFactory.Unsafe(ctx, "invalid_market_data"));

            return Task.CompletedTask;
        }

        if (ctx.Input.Volume < 0)
        {
            Logger.LogWarning("Invalid volume detected for {Symbol}. Volume: {Volume}", ctx.Input.Symbol, ctx.Input.Volume);

            ctx.Terminate(outcomeFactory.Unsafe(ctx, "invalid_market_data"));

            return Task.CompletedTask;
        }

        ctx.AddStep(AgentConstants.StepInputValidation, ctx.Input.Symbol, JsonSerializer.Serialize(new
        {
            ctx.Input.Price,
            ctx.Input.Volume,
            ctx.Input.ChangePercent
        }));

        Logger.LogInformation("Input validation passed for {Symbol}", ctx.Input.Symbol);

        return Task.CompletedTask;
    }
}