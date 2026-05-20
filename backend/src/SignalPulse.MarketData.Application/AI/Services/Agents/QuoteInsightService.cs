using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class QuoteInsightService(IKernelInvoker kernelInvoker,
    ILogger<QuoteInsightService> logger)
{
    public async Task<AIInsightResult> AnalyzeAsync(string symbol, decimal price, decimal changePercent, long volume, CancellationToken ct = default)
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
                ["symbol"] = symbol,
                ["price"] = price,
                ["changePercent"] = changePercent,
                ["volume"] = volume
            };

            var raw = await kernelInvoker.InvokeAsync(AgentConstants.QuoteInsightSkill, args, ct);

            if (string.IsNullOrWhiteSpace(raw))
            {
                logger.LogWarning("Quote insight returned empty response for {Symbol}", symbol);

                return SafeFallback("empty_llm_response");
            }

            AIInsightResult? parsed;

            try
            {
                parsed = JsonSerializer.Deserialize<AIInsightResult>(raw, AiJson.Options);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Quote insight returned invalid JSON for {Symbol}", symbol);

                return SafeFallback("invalid_json_from_llm");
            }

            if (parsed is null)
            {
                logger.LogWarning("Quote insight returned null result for {Symbol}", symbol);

                return SafeFallback("null_deserialization");
            }

            return parsed;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Quote insight cancelled for {Symbol}", symbol);

            return SafeFallback("operation_cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quote insight failed for {Symbol}", symbol);

            return SafeFallback("quote_insight_failed");
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