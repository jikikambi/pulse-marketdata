using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public class MockForexInsightProvider : IAiInsightProvider<ForexInsightInput>
{
    public Task<AIInsightResult> GenerateAsync(ForexInsightInput input, CancellationToken ct = default)
    {
        var (sentiment, direction, volatility) = MockInsightGenerator.Generate();

        var pair = $"{input.FromSymbol}/{input.ToSymbol}";

        var explanation =
            $"{pair} is trending {direction} with a {sentiment} sentiment. " +
            $"Market volatility appears {volatility}. " +
            $"Open: {input.Open:F5}, High: {input.High:F5}, Low: {input.Low:F5}, Close: {input.Close:F5}.";

        return Task.FromResult(new AIInsightResult(
            Enum.Parse<SentimentType>(sentiment, true),
            Enum.Parse<DirectionType>(direction, true),
            Enum.Parse<VolatilityType>(volatility, true),
            explanation));
    }
}