using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SignalPulse.MarketData.Application.AI.Models;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Prompts;

public sealed class ForexInsightPrompt(Kernel kernel)
{
    private static readonly string path = Path.Combine(AppContext.BaseDirectory, AgentConstants.PromptPath);
    private readonly KernelPlugin _plugin = kernel.CreatePluginFromPromptDirectory(path);

    public async Task<AIInsightResult> AnalyzeAsync(string fromSymbol, string toSymbol, decimal open, decimal high, decimal low, decimal close, CancellationToken ct = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.2,
            MaxTokens = 300
        };

        var args = new KernelArguments
        {
            ["fromSymbol"] = fromSymbol,
            ["toSymbol"] = toSymbol,
            ["open"] = open,
            ["high"] = high,
            ["low"] = low,
            ["close"] = close
        };

        var result = await kernel.InvokeAsync(_plugin["ForexInsight"], args, ct);

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
            Sentiment: "neutral",
            Direction: "sideways",
            Volatility: "low",
            Rationale: $"fallback: {reason}"
        );
}