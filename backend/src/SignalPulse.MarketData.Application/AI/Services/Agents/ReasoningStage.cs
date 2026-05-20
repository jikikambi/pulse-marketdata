using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ReasoningStage(IKernelInvoker kernelInvoker,
    IAsyncPolicy<string> retryPolicy,
    ILogger<ReasoningStage> logger, IWorkflowOutcomeFactory outcomeFactory)
    : MarketAgentStageBase<ReasoningStage>(logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public override MarketAgentStage Stage => MarketAgentStage.Reasoning;

    protected override async Task ExecuteInternalAsync(MarketAgentWorkflowContext ctx, CancellationToken ct)
    {
        Logger.LogInformation("Starting reasoning stage for {Symbol}", ctx.Input.Symbol);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        cts.CancelAfter(Timeout);

        try
        {
            var result = await retryPolicy.ExecuteAsync(async () =>
            {
                return await kernelInvoker.InvokeAsync(AgentConstants.ReasonerSkill, new KernelArguments
                {
                    ["symbol"] = ctx.Input.Symbol,
                    ["price"] = ctx.Input.Price,
                    ["changePercent"] = ctx.Input.ChangePercent,
                    ["volume"] = ctx.Input.Volume,
                    ["context"] = ctx.ToolContextJson ?? "null",
                    ["correlationId"] = ctx.Input.CorrelationId
                }, cts.Token);
            });

            var raw = result?.ToString();

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