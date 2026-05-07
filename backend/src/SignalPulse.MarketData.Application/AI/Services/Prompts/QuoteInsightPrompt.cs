using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Prompts;

public sealed class QuoteInsightPrompt(Kernel kernel)
{
    private static readonly string path = Path.Combine(AppContext.BaseDirectory, AgentConstants.PromptPath);
    private readonly KernelPlugin _plugin = kernel.CreatePluginFromPromptDirectory(path);

    public async Task<AIInsightResult> AnalyzeAsync(string symbol, decimal price, decimal changePercent, long volume, CancellationToken ct = default)
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

        var result = await kernel.InvokeAsync(_plugin["QuoteInsight"], args, ct);

        var json = result.GetValue<string>() ?? "{}";

        if (string.IsNullOrWhiteSpace(json))
            return SafeFallback("empty_llm_response");

        try
        {
            var parsed = JsonSerializer.Deserialize<AIInsightResult>(json);

            return parsed ?? SafeFallback("null_deserialization");
        }
        catch (JsonException)
        {
            return SafeFallback("invalid_json_from_llm");
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