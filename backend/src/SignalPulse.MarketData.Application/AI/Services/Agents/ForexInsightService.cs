using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ForexInsightService(IKernelInvoker kernelInvoker,
    ILogger<ForexInsightService> logger)
{
    public async Task<AIInsightResult> AnalyzeAsync(string fromSymbol, string toSymbol, decimal open, decimal high, decimal low, decimal close, CancellationToken ct = default)
    {
        try
        {
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.2,
                MaxTokens = 300
            };

            var args = new KernelArguments(settings)
            {
                ["fromSymbol"] = fromSymbol,
                ["toSymbol"] = toSymbol,
                ["open"] = open,
                ["high"] = high,
                ["low"] = low,
                ["close"] = close
            };

            var raw = await kernelInvoker.InvokeAsync(AgentConstants.ForexInsightSkill, args, ct);

            if (string.IsNullOrWhiteSpace(raw))
            {
                logger.LogWarning("FX insight returned empty response for {From}/{To}", fromSymbol, toSymbol);

                return SafeFallback("empty_llm_response");
            }

            AIInsightResult? parsed;

            try
            {
                parsed = JsonSerializer.Deserialize<AIInsightResult>(raw, AiJson.Options);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "FX insight returned invalid JSON for {From}/{To}", fromSymbol, toSymbol);

                return SafeFallback("invalid_json_from_llm");
            }

            if (parsed is null)
            {
                logger.LogWarning("FX insight returned null result for {From}/{To}", fromSymbol, toSymbol);

                return SafeFallback("null_deserialization");
            }

            return parsed;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("FX insight cancelled for {From}/{To}", fromSymbol, toSymbol);

            return SafeFallback("operation_cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FX insight failed for {From}/{To}", fromSymbol, toSymbol);

            return SafeFallback("fx_insight_failed");
        }
    }

    private static AIInsightResult SafeFallback(string reason)
        => new(
            Sentiment: SentimentType.Neutral,
            Direction: DirectionType.Sideways,
            Volatility: VolatilityType.Low,
            Rationale: $"fallback: {reason}"
        );
}