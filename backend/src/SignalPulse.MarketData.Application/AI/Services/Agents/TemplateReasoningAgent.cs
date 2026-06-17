using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public class TemplateReasoningAgent(MockQuoteInsightProvider provider) : IReasoningAgent
{
    public string Name => "template";

    public async Task<string?> GenerateAsync(QuoteInsightInput input, string? toolContext, CancellationToken ct)
    {
        var insight = await provider.GenerateAsync(input, ct);

        return JsonSerializer.Serialize(insight)!;
    }
}