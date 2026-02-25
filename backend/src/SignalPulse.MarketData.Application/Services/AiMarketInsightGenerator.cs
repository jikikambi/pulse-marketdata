using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.Services;

public class AiMarketInsightGenerator(QuoteInsightPrompt prompt) : IAiMarketInsightGenerator
{
    private readonly QuoteInsightPrompt _prompt = prompt;
    private static readonly Random _rand = new();

    public async Task<AIInsightResult> GenerateAsync(string symbol, decimal price, decimal changePercent, long volume, CancellationToken ct = default)
    {   
        //return await prompt.AnalyzeAsync(symbol, price, changePercent, volume, ct);

        return await MockedAnalyzeAsync(symbol);
    }

    private static async Task<AIInsightResult> MockedAnalyzeAsync(string symbol)
    {
        // Fake but realistic logic
        var directions = new[] { "up", "down", "sideways" };
        var sentiments = new[] { "bullish", "bearish", "neutral" };
        var volLevels = new[] { "low", "medium", "high" };

        var result = new AIInsightResult(
            sentiments[_rand.Next(sentiments.Length)],
            directions[_rand.Next(directions.Length)],
            volLevels[_rand.Next(volLevels.Length)],
            $"{symbol} shows normal market behavior. This is mock insight."
            );

        return await Task.FromResult(result);
    }
}