using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI;
using System.Text.Json;

namespace SignalPulse.MarketData.Application;

public sealed class QuoteInsightPrompt(Kernel kernel)
{
    private static readonly string path = Path.Combine(AppContext.BaseDirectory, "Prompts");
    private readonly KernelPlugin _plugin = kernel.CreatePluginFromPromptDirectory(path);

    public async Task<AIInsightResult> AnalyzeAsync(string symbol, decimal price, decimal changePercent, long volume, CancellationToken ct = default)
    {
        var result = await kernel.InvokeAsync(_plugin["QuoteInsight"], new KernelArguments
        {
            ["symbol"] = symbol,
            ["price"] = price,
            ["changePercent"] = changePercent,
            ["volume"] = volume
        }, ct);

        var json = result.GetValue<string>() ?? "{}";
        return JsonSerializer.Deserialize<AIInsightResult>(json)!;
    }
}
