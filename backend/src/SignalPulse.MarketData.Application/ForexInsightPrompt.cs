using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI;
using System.Text.Json;

namespace SignalPulse.MarketData.Application;

public sealed class ForexInsightPrompt(Kernel kernel)
{
    private static readonly string path = Path.Combine(AppContext.BaseDirectory, "Prompts");
    private readonly KernelPlugin _plugin = kernel.CreatePluginFromPromptDirectory(path);

    public async Task<AIInsightResult> AnalyzeAsync( string fromSymbol, string toSymbol, decimal open, decimal high, decimal low, decimal close, CancellationToken ct = default)
    {
        var result = await kernel.InvokeAsync(_plugin["ForexInsight"], new KernelArguments
        {
            ["fromSymbol"] = fromSymbol,
            ["toSymbol"] = toSymbol,
            ["open"] = open,
            ["high"] = high,
            ["low"] = low,
            ["close"] = close
        }, ct);

        var json = result.GetValue<string>() ?? "{}";
        return JsonSerializer.Deserialize<AIInsightResult>(json)!;
    }
}