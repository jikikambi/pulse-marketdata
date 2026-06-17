using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Diagnostics;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ReasoningStage(ILogger<ReasoningStage> logger,
    IWorkflowOutcomeFactory outcomeFactory,
    IReasoningAgentResolver resolver)
    : MarketAgentStageBase<ReasoningStage>(logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public override MarketAgentStage Stage => MarketAgentStage.Reasoning;
    public override IReadOnlyCollection<MarketAgentStage> DependsOn => [MarketAgentStage.PlanParsing];

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        Logger.LogInformation("Starting reasoning stage for {Symbol}", ctx.Input.Symbol);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        cts.CancelAfter(Timeout);

        try
        {
            string? raw;

            try
            {
                var primary = resolver.GetPrimary();
                raw = await primary.GenerateAsync(ctx.Input, ctx.ToolContextJson, cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Primary reasoning agent failed for {Symbol}. Switching to alternate agent.", ctx.Input.Symbol);

                var alternate = resolver.GetFallback();

                if (alternate is null)
                {
                    Logger.LogError("No alternate reasoning agent available for {Symbol}", ctx.Input.Symbol);
                    ctx.Terminate(outcomeFactory.Safe(ctx, "reasoner_no_alternate"));
                    return;
                }

                raw = await alternate.GenerateAsync(ctx.Input, ctx.ToolContextJson, cts.Token);

                ctx.State.AlternateAgentsUsed[Stage] = alternate.Name;

                await ctx.EmitAsync(Stage.ToString(), "alternate_agent_used", alternate.Name, new
                {
                    Stage = Stage.ToString(),
                    Agent = alternate.Name
                }, ct);

                ObservabilityMetrics.Agent.AlternateAgentUsed.Add(1, new TagList
                {
                    { "stage", Stage.ToString() },
                    { "agent", alternate.Name }
                });
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                Logger.LogWarning("Reasoner returned empty response for {Symbol}", ctx.Input.Symbol);
                ctx.Terminate(outcomeFactory.Safe(ctx, "reasoner_empty_response"));

                return;
            }

            AIInsightResult? insight;

            try
            {
                insight = JsonSerializer.Deserialize<AIInsightResult>(raw, AiJson.Options);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "Reasoner returned invalid JSON for {Symbol}", ctx.Input.Symbol);

                ctx.Terminate(outcomeFactory.Safe(ctx, "reasoner_invalid_json"));

                return;
            }

            if (insight is null)
            {
                Logger.LogWarning("Reasoner returned null insight for {Symbol}", ctx.Input.Symbol);

                ctx.Terminate(outcomeFactory.Safe(ctx, "reasoner_null_result"));

                return;
            }

            ctx.Insight = insight;

            ctx.AddStep(AgentConstants.StepReasoner, ctx.ToolContextJson ?? "null", JsonSerializer.Serialize(insight));

            Logger.LogInformation("Reasoning stage completed successfully for {Symbol}. Sentiment: {Sentiment}, Direction: {Direction}", ctx.Input.Symbol, insight.Sentiment, insight.Direction);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Logger.LogWarning("Reasoner timeout after {TimeoutMs}ms for {Symbol}", Timeout.TotalMilliseconds, ctx.Input.Symbol);

            ctx.Terminate(outcomeFactory.Safe(ctx, "reasoner_timeout"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reasoner failed for {Symbol}", ctx.Input.Symbol);

            ctx.Terminate(outcomeFactory.Safe(ctx, "reasoner_failed"));
        }
    }
}