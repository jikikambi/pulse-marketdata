using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.Services;

public class MockForexInsightProvider : IAiInsightProvider<ForexInsightInput>
{
    private static readonly Random _rand = new();

    public Task<AIInsightResult> GenerateAsync(ForexInsightInput input, CancellationToken ct = default)
    {
        var pair = $"{input.FromSymbol}/{input.ToSymbol}";

        var explanation =
            $"{pair} is trending {MockInsightGenerator.Generate().direction} with a {MockInsightGenerator.Generate().sentiment} sentiment. " +
            $"Market volatility appears {MockInsightGenerator.Generate().volatility}. " +
            $"Open: {input.Open:F5}, High: {input.High:F5}, Low: {input.Low:F5}, Close: {input.Close:F5}.";

        return Task.FromResult(new AIInsightResult(MockInsightGenerator.Generate().sentiment, MockInsightGenerator.Generate().direction, MockInsightGenerator.Generate().volatility, explanation));
    }
}